using Api.Models;

namespace Api.Services;

public interface IWeatherService
{
    public Task<MyWeatherResponse> GetForecastAsync(
        MyWeatherRequest request,
        CancellationToken cancellationToken
    );
}
