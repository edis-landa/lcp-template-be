using LCP.Template.BE.Engines;
using LCP.Template.BE.Interfaces.Engines;

namespace LCP.Template.BE.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);

        services.AddEngines();

        return services;
    }

    private static IServiceCollection AddEngines(this IServiceCollection services)
    {
        services.AddScoped<IWeatherForecastEngine, WeatherForecastEngine>();

        return services;
    }
}
