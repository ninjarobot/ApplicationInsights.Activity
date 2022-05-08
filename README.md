AppInsightsActivityListener
========

The `AppInsightsActivityListener` listens for standard .NET System.Diagnostics.Activity and creates ApplicationInsights telemetry that corresponds to it, reducing the effort needed to instrument code.

The .NET libraries already emit the telemetry if there is a listener, so just adding this listener will send telemetry to an Application Insights workspace through the use of standard libraries such as the `HttpClient`. Adding you can create your own `Activity` instances to instrument your code as well, and it will be correlated within the disposable `Activity` instance's lifetime.

### Supported Features

* Traces
* Events
* Baggage
* Tags

Traces are created for any `ILogger` calls and will be correlated if they are created within a parent `Activity`. An `Activity` may also create `Events` that occur throughout the course of the `Activity` and these will send events to Application Insights that are correlated with the `Activity`.

It's often useful to enrich an Activity with metadata to enable searching the telemetry, so `Activity` supports setting `Baggage` and `Tags`. Because baggage is passed down to nested activities, baggage created in a parent will be included in all the Application Insights telemetry created within that parent's scope. Tags are not passed down to child scopes.

### Usage

```fsharp
let services = ServiceCollection()
services.AddAppInsightsActivityListener(fun opts -> opts.AddSource("MyApp").AddSource("System.Net.Http") |> ignore)
        .AddLogging(fun builder ->
            builder.SetMinimumLevel(LogLevel.Debug)
                .AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Information)
                .AddFilter<ApplicationInsightsLoggerProvider>("Category", LogLevel.Information)
                |> ignore) |> ignore
services.AddApplicationInsightsTelemetryWorkerService(fun options -> options.ConnectionString <- appInsightsConnStr) |> ignore
let serviceProvider = services.BuildServiceProvider()

let logger = serviceProvider.GetRequiredService<ILogger<Test>>()
let telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>()
let activityListener = serviceProvider.GetRequiredService<AppInsightsActivityListener>()
```