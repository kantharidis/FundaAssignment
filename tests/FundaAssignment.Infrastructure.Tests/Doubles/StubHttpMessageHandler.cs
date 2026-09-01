using System.Net;
using System.Text;

namespace FundaAssignment.Infrastructure.Tests.Doubles;

/// <summary>
/// Answers every request with the one response it was built with, and remembers what was asked for.
/// </summary>
internal sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string body, string mediaType)
    : HttpMessageHandler
{
    internal static StubHttpMessageHandler Serving(string json) =>
        new(HttpStatusCode.OK, json, "application/json");

    internal Uri? ReceivedUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ReceivedUri = request.RequestUri;

        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType),
        });
    }
}
