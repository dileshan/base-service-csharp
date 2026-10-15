using Api.Exceptions;
using Api.Middleware;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Model binding/validation failures use the same error body as the exception middleware.
        options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(
            ErrorResponse.FromModelState(context.ModelState)
        );
    });

builder.Services.Configure<OpenMeteoOptions>(
    builder.Configuration.GetSection(OpenMeteoOptions.SectionName)
);
builder.Services.AddHttpClient<IWeatherService, WeatherService>(
    (services, client) =>
    {
        var options = services.GetRequiredService<IOptions<OpenMeteoOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    }
);

var app = builder.Build();

// Must be first so it wraps everything else in the pipeline.
app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();

// Unmatched routes become a 404 with the standard error body.
app.MapFallback(context =>
    throw new NotFoundException(
        $"Endpoint does not exist for {context.Request.Method} {context.Request.Path}."
    )
);

app.Run();
