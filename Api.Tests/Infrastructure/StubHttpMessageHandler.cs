using System.Net;
using System.Text;

namespace Api.Tests.Infrastructure;

/// <summary>Returns a canned response and records the request, for testing typed HttpClient services.</summary>
public sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string body)
    : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        LastRequest = request;
        return Task.FromResult(
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            }
        );
    }
}
