namespace Api.Exceptions;

public sealed class NotFoundException(
    string message,
    string? target = null,
    Exception? innerException = null
) : ApiException(StatusCodes.Status404NotFound, message, target, innerException);
