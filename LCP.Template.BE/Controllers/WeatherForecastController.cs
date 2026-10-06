using LCP.Template.BE.Interfaces.Engines;
using LCP.Template.BE.ModelsDto.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LCP.Template.BE.Controllers;

// TODO: Replace with [Authorize(Policy = ...)] once Entra ID is configured (see CODING_STANDARDS.md §20).
[ApiController]
[AllowAnonymous]
[Produces("application/json")]
[Route("service/api/v1/weather-forecasts")]
public partial class WeatherForecastController(
    IWeatherForecastEngine weatherForecastEngine,
    ILogger<WeatherForecastController> logger) : ControllerBase
{
    /// <summary>
    /// Gets the weather forecast for the next five days.
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The daily forecasts, ordered by date.</returns>
    /// <response code="200">The forecasts were returned.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WeatherForecastResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WeatherForecastResponse>>> GetForecastsAsync(CancellationToken cancellationToken)
    {
        LogGettingForecasts();

        IEnumerable<WeatherForecastResponse> forecasts = await weatherForecastEngine.GetForecastsAsync(cancellationToken);

        LogForecastsReturned();

        return Ok(forecasts);
    }

    [LoggerMessage(LogLevel.Information, "Getting weather forecasts.")]
    partial void LogGettingForecasts();

    [LoggerMessage(LogLevel.Information, "Weather forecasts returned.")]
    partial void LogForecastsReturned();
}
