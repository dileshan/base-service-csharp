using System.Net;
using Api.Controllers;
using Api.Tests.Infrastructure;

namespace Api.Tests.Controllers;

public sealed class HealthControllerTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void Get_ReturnsHealthyPlainText()
    {
        var result = new HealthController().Get();

        Assert.Equal("healthy", result.Content);
        Assert.Equal("text/plain", result.ContentType);
    }

    [Fact]
    public async Task GetHealth_Returns200WithHealthyBody()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("healthy", await response.Content.ReadAsStringAsync());
    }
}
