namespace LCP.Template.BE.ModelsDto.Responses;

/// <summary>
/// A weather forecast for a single day.
/// </summary>
public record WeatherForecastResponse
{
    /// <summary>
    /// The forecast date.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// The forecast temperature in degrees Celsius.
    /// </summary>
    public required int TemperatureC { get; init; }

    /// <summary>
    /// The forecast temperature in degrees Fahrenheit.
    /// </summary>
    public required int TemperatureF { get; init; }

    /// <summary>
    /// A short description of the weather, for example "Mild" or "Hot".
    /// </summary>
    public string? Summary { get; init; }
}
