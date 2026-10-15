using Api.Exceptions;
using Api.Models;

namespace Api.Middleware;

/// <summary>
/// Catches every exception thrown further down the pipeline and writes an <see cref="ErrorResponse"/>.
/// Known <see cref="ApiException"/> types keep their status code; anything else becomes a 500.
/// </summary>
public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public const string UnexpectedErrorMessage = "An unexpected error occurred.";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected, so there is nobody left to send a response to.
            logger.LogDebug(
                "Request {Method} {Path} was cancelled by the client.",
                context.Request.Method,
                context.Request.Path
            );
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(
                    exception,
                    "Unhandled exception after the response started for {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path
                );
                throw;
            }

            await WriteErrorAsync(context, exception);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";

        var error = exception switch
        {
            ApiException apiException => new ErrorResponse(
                apiException.StatusCode,
                apiException.Message,
                apiException.Target ?? path
            ),
            BadHttpRequestException badRequest => new ErrorResponse(
                StatusCodes.Status400BadRequest,
                badRequest.Message,
                path
            ),
            _ => new ErrorResponse(
                StatusCodes.Status500InternalServerError,
                UnexpectedErrorMessage,
                path
            ),
        };

        if (error.Code >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}.",
                context.Request.Method,
                path
            );
        }
        else
        {
            logger.LogInformation(
                "Request {Method} {Path} failed with {StatusCode}: {Message}",
                context.Request.Method,
                path,
                error.Code,
                error.Message
            );
        }

        context.Response.Clear();
        context.Response.StatusCode = error.Code;
        await context.Response.WriteAsJsonAsync(error, context.RequestAborted);
    }
}
