using System.Collections.Concurrent;
using System.Diagnostics;
using ApplicationInsights.Activity.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;

namespace ApplicationInsights.Activity;
public class AppInsightsActivityListener
{
    private readonly ConcurrentDictionary<System.Diagnostics.Activity, IOperationHolder<RequestTelemetry>?> _operations = new ();
    private readonly TelemetryClient _telemetryClient;

    private void addBaggage(IOperationHolder<RequestTelemetry> operation, System.Diagnostics.Activity activity)
    {
        foreach (var baggage in activity.Baggage) {
            // add baggage from parent if not present in child already
            if (!operation.Telemetry.Properties.ContainsKey(baggage.Key))
            {
                operation.Telemetry.Properties.Add(baggage.Key, baggage.Value);
                if (activity.Parent != null)
                {
                    addBaggage(operation, activity.Parent);
                }
            }
        }
    }

    private void StartOperation(System.Diagnostics.Activity activity)
    {
        var operation = _telemetryClient.StartOperation<RequestTelemetry>(activity);
        addBaggage(operation, activity);
        foreach (var tag in activity.Tags)
        {
            operation.Telemetry.Properties.Add(tag.Key, tag.Value);
        }
        _operations.TryAdd(activity, operation);
    }

    private void StopOperation(System.Diagnostics.Activity activity)
    {
        if (_operations.TryRemove(activity, out var operation))
        {
            foreach (var e in activity.Events)
            {
                var evt = new EventTelemetry(e.Name)
                {
                    Timestamp = e.Timestamp
                };
                foreach (var tag in e.Tags)
                    evt.Properties.Add(tag.Key, tag.Value?.ToString());
                _telemetryClient.TrackEvent(evt);
            }
            operation?.Dispose();
        }
    }

    public AppInsightsActivityListener(TelemetryClient telemetryClient, IOptions<AppInsightsActivityListenerOptions> config)
    {
        _telemetryClient = telemetryClient;
        var sources = config.Value.Sources;
        ActivityListener activityListener = new()
        {
            ActivityStarted = activity => StartOperation(activity),
            ActivityStopped = activity => StopOperation(activity),
            ShouldListenTo = (activitySource) => sources.Count == 0 || sources.Contains(activitySource.Name),
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(activityListener);
    }
}
