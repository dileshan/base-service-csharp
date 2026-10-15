using System.Text.Json;
using Api.Exceptions;
using Api.Middleware;
using Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace Api.Tests.Middleware;

public sealed class ExceptionMiddlewareTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static async Task<(HttpContext Context, ErrorResponse? Error)> InvokeAsync(
        RequestDelegate next,
        string path = "/myweather"
    )
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        await new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance).InvokeAsync(
            context
        );

        context.Response.Body.Position = 0;
        var error =
            context.Response.Body.Length == 0
                ? null
                : await JsonSerializer.DeserializeAsync<ErrorResponse>(
                    context.Response.Body,
                    WebJson
                );
        return (context, error);
    }

    [Fact]
    public async Task NoException_PassesThrough()
    {
        var (context, error) = await InvokeAsync(ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.Null(error);
    }

    [Fact]
    public async Task BadRequestException_Returns400WithMessageAndTarget()
    {
        var (context, error) = await InvokeAsync(_ =>
            throw new BadRequestException("Bad latitude.", "latitude")
        );

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(new ErrorResponse(400, "Bad latitude.", "latitude"), error);
    }

    [Fact]
    public async Task NotFoundException_WithoutTarget_UsesRequestPath()
    {
        var (context, error) = await InvokeAsync(
            _ => throw new NotFoundException("Nothing here."),
            "/missing"
        );

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal(new ErrorResponse(404, "Nothing here.", "/missing"), error);
    }

    [Fact]
    public async Task BadHttpRequestException_Returns400()
    {
        var (context, error) = await InvokeAsync(_ =>
            throw new BadHttpRequestException("Malformed body.")
        );

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(new ErrorResponse(400, "Malformed body.", "/myweather"), error);
    }

    [Fact]
    public async Task UnknownException_Returns500WithoutLeakingDetails()
    {
        var (context, error) = await InvokeAsync(_ =>
            throw new InvalidOperationException("secret connection string")
        );

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        Assert.Equal(
            new ErrorResponse(500, ExceptionMiddleware.UnexpectedErrorMessage, "/myweather"),
            error
        );
    }

    [Fact]
    public async Task ClientCancellation_WritesNothing()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        var context = new DefaultHttpContext { RequestAborted = aborted.Token };
        context.Response.Body = new MemoryStream();

        await new ExceptionMiddleware(
            _ => throw new OperationCanceledException(),
            NullLogger<ExceptionMiddleware>.Instance
        ).InvokeAsync(context);

        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task ResponseAlreadyStarted_Rethrows()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        var middleware = new ExceptionMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<ExceptionMiddleware>.Instance
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
    }

    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }
}
