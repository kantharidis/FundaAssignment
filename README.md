## Prerequisites

- .NET 10 SDK
- A funda partner API key, supplied as an environment variable. It is not in the repository -
  `appsettings.json` is committed, and the key is a path segment of every request URI.

## Running it

Set the key first. The variable name is the configuration path with two underscores between each
part, which is how .NET maps environment variables onto `Funda:Client:ApiKey`:

```powershell
$env:Funda__Client__ApiKey = "your-key-here"
```

```bash
export Funda__Client__ApiKey="your-key-here"
```

That lasts for the current shell session.

Then:

```
dotnet build FundaAssignment.slnx
dotnet run --project src/FundaAssignment.Cli
```

Forget the key and the app stops before it makes a request, with exit code 2 and:

```
Configuration is not usable: DataAnnotation validation failed for 'FundaClientOptions'
members: 'ApiKey' with the error: 'No funda API key. Set the environment variable
Funda__Client__ApiKey (two underscores between each part).'
```

You get a menu, and the process stays alive:

```
  1  Agents with the most properties for sale in Amsterdam
  2  ...and the same for properties with a garden
  3  Both
  q  Quit
```

Both rankings are top 10, and a full pass over Amsterdam is roughly 200 requests. Because funda
allows 100 requests a minute, the app spaces them out deliberately - expect a couple of minutes
for a cold run. Pick the same ranking again inside 15 minutes and it comes back instantly from
the cache; the app tells you which happened:

```
All 187 pages came from the cache - nothing was asked of funda.
```

Logs go to the console and to a rolling file next to the executable, at
`logs/funda-rank-<date>.log`, seven days retained. The path is printed to stderr on startup.

Exit codes: `0` ran, `1` funda could not be read or something unexpected happened, `2`
configuration is unusable, `130` Ctrl+C.

## Configuration

Three sections, each bound to the one options class that reads it, so a constructor names exactly
the settings it consults and a change to the retry policy cannot reach the URL builder. All of it
is in `appsettings.json`.

```
$env:Funda__Client__ApiKey = "..."
dotnet run --project src/FundaAssignment.Cli
```

| Section | Class | Settings |
| --- | --- | --- |
| `Funda:Client` | `FundaClientOptions` | `ApiKey` (**required, environment only**), `BaseAddress`, `PageSize` 1-500, `RequestTimeout` |
| `Funda:Resilience` | `FundaResilienceOptions` | `RequestsPerMinute` 1-100, `MaxRetryAttempts` 0-10 |
| `Funda:Caching` | `FundaCachingOptions` | `Enabled`, `SnapshotWindow` |
| `Logging:File` | read by `LogFile` | `Path`, relative to the executable |

`Funda:Caching:Enabled = false` turns the cache off without changing anything structural - the
decorator stays in place and passes straight through. It is the first thing to reach for when a
ranking looks wrong.

## How it fits together

Four projects, references pointing inwards only:

```
Cli -> Infrastructure -> Application -> Domain
```

| Project | Role |
| --- | --- |
| `src/FundaAssignment.Domain` | Listings, agents, and the function that ranks them. Plain records, no project or package references at all. |
| `src/FundaAssignment.Application` | The use case and the ports it needs. Knows Domain, knows nothing about funda. |
| `src/FundaAssignment.Infrastructure` | Everything funda: URL grammar, wire contracts, mapping, resilience, caching. |
| `src/FundaAssignment.Cli` | Composition root, menu, markdown output, logging, exit codes. |
| `tests/FundaAssignment.Architecture.Tests` | Enforces the table above, so the layering cannot rot quietly. |

The interesting part is that the reference arrow and the call arrow point in opposite directions
across the Application/Infrastructure boundary. `IListingSource` is declared in Application and
implemented in Infrastructure, so at run time the handler calls *out* through a port it owns and
lands in the adapter - which is what lets the use case be tested against a stub and never learn
that funda exists.

One ranking, end to end:

```
RankingMenu (Cli)
  -> RankAgentsHandler (Application)      drains IListingSource into a list
    -> FundaListingSource (Infrastructure) pages the search, yields listings
      -> the client chain                  cache -> retry + rate limit -> HTTP
      -> FundaContractMapper               wire contract -> domain Listing
  -> RankingCalculator (Domain)            group by agent, order, take 10
  -> MarkdownRankingTable (Cli)            render
```

Inside each project, folders group files by what kind of thing they are, and the folder name is
the namespace - `Domain/{Models,Services}`, `Application/{Ports,Queries,Handlers,Dtos,Mapping}`,
`Infrastructure/Funda/{Client,Contracts,Requests,Mapping,Resilience,Caching,Configuration}`,
`Cli/{Menu,Rendering,Hosting}`. Each test project mirrors the one it covers.

### The funda client chain

`IFundaFeedClient` is one internal interface with one method - fetch one page - and three
implementations, two of which take another one as a constructor argument. It is a decorator chain,
assembled once in `AddFundaFeed`:

```
FundaListingSource
  -> CachingFundaFeedClient      remembers pages; key = 15-min window + search + page + size
    -> ResilientFundaFeedClient  retry (3, 1s doubling) around a token bucket, 1 token / 667 ms
      -> FundaFeedClient         builds the URI, GETs it, deserialises, maps to domain records
        -> partnerapi.funda.nl
```

Two ordering decisions carry most of the weight:

- **The cache is outermost, so a hit spends no rate-limit token.** The limiter deliberately wraps
  the *operation* rather than the HTTP handler. If the cache sat underneath it - as a
  `DelegatingHandler` would - a hit would queue for a token and wait its 667 ms before discovering
  it had nothing to send, which is the whole value of the cache gone.
- **Retry sees the parsed envelope, not the HTTP response.** funda refuses requests inside a
  200 OK body, so the retryable unit is fetch-plus-deserialise-plus-map. By the time the retry
  pipeline sees an outcome, a refusal has already become an exception it can match on. A
  status-code-only pipeline would read a throttled request as an empty page and silently
  undercount.

Each link is registered under a private key rather than under its own type, so the only
`IFundaFeedClient` anything can resolve is the assembled chain. Asking for a bare `FundaFeedClient`
gets you nothing, rather than a client with no rate limiter and no cache in front of it.

Failures stop being funda's at the port. `FundaRejectedRequestException` is internal and exists so
the retry pipeline has something to match on; `FundaListingSource` translates it - and
`HttpRequestException` - into `ListingSourceUnavailableException` on the way out, above the
retries, so the pipeline still sees the funda type and the CLI never does.

### Model families

Four families keep the boundaries visible, and the architecture tests enforce the naming that
separates them:

| Family | Lives in | Suffix | Rules |
| --- | --- | --- | --- |
| Query | Application | `*Query` | Immutable, business vocabulary |
| Domain model | Domain | none | Plain records, no logic, no attributes. Rules live in `RankingCalculator`. |
| API request | Infrastructure | `*Request` | Builds the funda URL: `type`, `zo`, `page`, `pagesize` |
| Wire contract | Infrastructure | `*Response`, `*Payload` | Everything nullable, Dutch names, `internal`, no behaviour |
| Output DTO | Application | `*Dto`, `*Result` | Flat and presentation-shaped |

## Notes on the design

- **Architecture tests read the `.csproj` files, not compiled metadata.** The compiler prunes
  references that are declared but unused, so an unwanted `ProjectReference` would stay invisible
  to reflection until somebody wrote code against it.
- **The wire contracts model 3 of roughly 110 fields.** System.Text.Json skips the rest without
  materialising them, so parsing cost scales with what we model - and none of the feed's awkward
  corners (`/Date(...)/` timestamps, `Soort-aanbod`, HTML inside the price fields) needs handling
  at all.
- **Mapping is hand-written.** There are exactly two mapping seams, and they are the code most
  worth reading. A reflection-based mapper would fail at run time where these fail at compile time.
- **Pagination is sequential.** Parallel fan-out gains nothing when the rate limiter serialises
  requests anyway, and a page-by-page loop is easier to read, test and debug.
- **Every listing handed to the ranking is counted.** funda pages by offset, so a listing that
  moved mid-run could in principle be served twice. The run logs its own count
  against `TotaalAantalObjecten`, which is what would reveal it either way.
- **The API key never leaves the URL builder.** It is an argument to `FeedPageRequest.ToUri`, not
  a property, so it cannot end up in a record's `ToString`, a log line, or a cache key. `HttpClient`
  logging is filtered to Warning for the same reason - it logs the request URI, and the key is a
  path segment of it.
- **The Visual Studio "connected service" was removed.** It generated an 11k-line WCF/SOAP client
  from `Aanbod.svc?wsdl`, plus five `System.ServiceModel` packages. Worth keeping from it: the
  WSDL names the search operation
  `ZoekAanbod(key, aanbodType, zoekPad, since, page, pagesize, statistiekId, projectObjectenTonen)`

## Known gaps

- The cache is memory-backed, so it only pays off within a single process. Making it survive a
  restart is a one-line registration change in `Program.cs`; nothing in the chain moves.
- The retry predicate treats any refusal as retryable. Once a real throttled response has been
  captured it should narrow to what was actually observed.
- `pagesize` stays at funda's documented 25.
