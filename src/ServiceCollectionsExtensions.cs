using Microsoft.Extensions.DependencyInjection;

namespace ApplicationInsights.Activity.Extensions.DependencyInjection;

public class AppInsightsActivityListenerOptions
{
    private readonly List<string> _sources = new();

    public AppInsightsActivityListenerOptions AddSource(string serviceName)
    {
        _sources.Add(serviceName);
        return this;
    }

    public List<string> Sources
    {
        get { return this._sources; }
    }
}

public static class ServiceCollectionsExtensions
{
    public static IServiceCollection AddAppInsightsActivityListener(this IServiceCollection services)
    {
        services.AddOptions<AppInsightsActivityListenerOptions>().Configure(_ => { });
        return services.AddSingleton<AppInsightsActivityListener>();
    }
    public static IServiceCollection AddAppInsightsActivityListener(this IServiceCollection services, Action<AppInsightsActivityListenerOptions> options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options), "Missing options for AppInsightsActivityListener.");
        services.Configure(options);
        return services.AddSingleton<AppInsightsActivityListener>();
    }
}