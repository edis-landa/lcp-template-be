using LCP.Template.BE.Interfaces.Engines;
using LCP.Template.BE.Models;
using LCP.Template.BE.ModelsDto.Responses;

namespace LCP.Template.BE.Engines;

public partial class WeatherForecastEngine(
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor,
    ILogger<WeatherForecastEngine> logger) : IWeatherForecastEngine
{
    private const int ForecastDays = 5;
    private const int MinTemperatureC = -20;
    private const int MaxTemperatureC = 55;

    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public Task<IEnumerable<WeatherForecastResponse>> GetForecastsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateOnly today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        WeatherForecastResponse[] forecasts = Enumerable.Range(1, ForecastDays)
            .Select(dayOffset => new WeatherForecast
            {
                Date = today.AddDays(dayOffset),
                TemperatureC = Random.Shared.Next(MinTemperatureC, MaxTemperatureC),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .Select(ToResponse)
            .ToArray();

        LogForecastsGenerated(forecasts.Length, httpContextAccessor.HttpContext?.TraceIdentifier ?? string.Empty);

        return Task.FromResult<IEnumerable<WeatherForecastResponse>>(forecasts);
    }

    private static WeatherForecastResponse ToResponse(WeatherForecast forecast) => new()
    {
        Date = forecast.Date,
        TemperatureC = forecast.TemperatureC,
        TemperatureF = ToFahrenheit(forecast.TemperatureC),
        Summary = forecast.Summary
    };

    private static int ToFahrenheit(int temperatureC) => 32 + (int)(temperatureC / 0.5556);

    [LoggerMessage(LogLevel.Information, "Weather forecasts generated. Count: {Count}, CorrelationId: {CorrelationId}")]
    partial void LogForecastsGenerated(int count, string correlationId);
}
