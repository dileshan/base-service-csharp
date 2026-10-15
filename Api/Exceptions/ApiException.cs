namespace Api.Exceptions;

/// <summary>
/// Base type for exceptions that map to a known HTTP status code. The exception middleware
/// turns these into an <see cref="Models.ErrorResponse"/> with the matching status code.
/// </summary>
public abstract class ApiException(
    int statusCode,
    string message,
    string? target = null,
    Exception? innerException = null
) : Exception(message, innerException)
{
    public int StatusCode { get; } = statusCode;

    /// <summary>The field, parameter or resource the error relates to. Defaults to the request path when null.</summary>
    public string? Target { get; } = target;
}
