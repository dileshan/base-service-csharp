namespace Api.Exceptions;

public sealed class BadRequestException(
    string message,
    string? target = null,
    Exception? innerException = null
) : ApiException(StatusCodes.Status400BadRequest, message, target, innerException);
