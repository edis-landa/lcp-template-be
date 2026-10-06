using LCP.Template.BE.ModelsDto.Responses;

namespace LCP.Template.BE.Interfaces.Engines;

public interface IWeatherForecastEngine
{
    /// <summary>
    /// Gets the forecast for the next five days, starting tomorrow.
    /// </summary>
    Task<IEnumerable<WeatherForecastResponse>> GetForecastsAsync(CancellationToken cancellationToken = default);
}
