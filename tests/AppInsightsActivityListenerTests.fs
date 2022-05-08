module Tests

open Expecto
open System
open System.Diagnostics
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.ApplicationInsights
open Microsoft.ApplicationInsights
open ApplicationInsights.Activity
open ApplicationInsights.Activity.Extensions.DependencyInjection

let config = ConfigurationBuilder().AddEnvironmentVariables().AddUserSecrets().Build()

let appInsightsConnStr = config.["AppInsightsConnectionString"]

let MyAppActivitySource = new ActivitySource("MyApp")

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

// Log some structured and unstructured data
let beachTrip (family:string, beachItems:string list) =
    use activity = MyAppActivitySource.CreateActivity("Beach Trip", ActivityKind.Internal)
    activity.Start() |> ignore
    activity.SetBaggage("Family", family) |> ignore
    logger.LogInformation("{Family} heading to the beach", family)
    for item in beachItems do
        logger.LogInformation("Bringing {Item}", item)
    System.Threading.Thread.Sleep 500
    activity.AddEvent(ActivityEvent("Rainfall")) |> ignore
    logger.LogError("It started raining")
    use activity2 = MyAppActivitySource.CreateActivity("Over", ActivityKind.Internal)
    activity2.Start() |> ignore
    use http = new System.Net.Http.HttpClient()
    http.GetStringAsync ("http://localhost:8080") |> Async.AwaitTask |> Async.Ignore |> Async.RunSynchronously
    System.Threading.Thread.Sleep 250
    logger.LogInformation("Heading back inside")
    activity2.Stop()
    activity.Stop()

[<Tests>]
let tests =
    testList "Basic traces" [
        testTask "basic trace 1" {
            beachTrip ("Brown", ["Shovel"; "Beach Toys"; "Chairs"; "Towels"])
            do! telemetryClient.FlushAsync Threading.CancellationToken.None
            do! System.Threading.Tasks.Task.Delay 2000
        }
        testTask "basic trace 2" {
            beachTrip ("Smith", ["Chairs"; "Towels"])
            do! telemetryClient.FlushAsync Threading.CancellationToken.None
            do! System.Threading.Tasks.Task.Delay 2000
        }
        testTask "basic trace 3" {
            beachTrip ("James", ["Chairs"; "Umbrella"; "Cooler"])
            do! telemetryClient.FlushAsync Threading.CancellationToken.None
            do! System.Threading.Tasks.Task.Delay 2000
        }
        testTask "basic trace 4" {
            beachTrip ("Miller", ["Sunglasses"; "Towels"])
            do! telemetryClient.FlushAsync Threading.CancellationToken.None
            do! System.Threading.Tasks.Task.Delay 2000
        }
        testTask "basic trace 5" {
            beachTrip ("Eggles", ["Chairs"; "Towels"; "Beach Toys"; "Umbrella"; "Cooler"])
            do! telemetryClient.FlushAsync Threading.CancellationToken.None
            do! System.Threading.Tasks.Task.Delay 2000
        }
    ]
