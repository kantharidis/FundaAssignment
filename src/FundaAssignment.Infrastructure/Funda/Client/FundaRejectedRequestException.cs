namespace FundaAssignment.Infrastructure.Funda.Client;

/// <summary>
/// funda accepted the HTTP request and then refused it in the body.
/// </summary>
internal sealed class FundaRejectedRequestException : Exception
{
    public FundaRejectedRequestException(string message)
        : base(message)
    {
    }

    public FundaRejectedRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
