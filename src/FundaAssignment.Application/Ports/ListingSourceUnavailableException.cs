namespace FundaAssignment.Application.Ports;

/// <summary>
/// The listing source could not produce the listings it was asked for.
/// </summary>
public sealed class ListingSourceUnavailableException : Exception
{
    public ListingSourceUnavailableException(string message)
        : base(message)
    {
    }

    public ListingSourceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
