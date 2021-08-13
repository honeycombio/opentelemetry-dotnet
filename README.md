# OpenTelemetry Exporter for .NET

[![OSS Lifecycle](https://img.shields.io/osslifecycle/honeycombio/opentelemetry-dotnet)](https://github.com/honeycombio/home/blob/main/honeycomb-oss-lifecycle-and-practices.md)

**STATUS: this project is being sunset.** See https://github.com/honeycombio/opentelemetry-dotnet/issues/14

## Setup

```
dotnet add package OpenTelemetry --version 0.2.0-alpha.179
dotnet add package OpenTelemetry.Collector.AspNetCore --version 0.2.0-alpha.179
dotnet add package OpenTelemetry.Collector.Dependencies --version 0.2.0-alpha.179
dotnet add package OpenTelemetry.Hosting --version 0.2.0-alpha.179
dotnet add package Honeycomb
dotnet add package Newtonsoft.Json
dotnet add package Honeycomb.OpenTelemetry --version 0.9.0-pre
```

`Startup.cs`
```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    // Honeycomb Setup
    services.Configure<HoneycombApiSettings>(Configuration.GetSection("HoneycombSettings"));
    services.AddHttpClient("honeycomb");
    services.AddSingleton<IHoneycombService, HoneycombService>();
    services.AddSingleton<HoneycombExporter>();

    // OpenTelemetry Setup
    services.AddOpenTelemetry((sp, builder) => {
        builder.UseHoneycomb(sp)
            .AddRequestCollector()
            .AddDependencyCollector();
    });
    ...
}
```

`appsettings.Development.json`
```json
{
  ...
  "HoneycombSettings": {
    "TeamId": "",
    "DefaultDataSet": "",
    "BatchSize": 100,
    "SendFrequency": 10000
  }
  ...
}
```


## Donation

This project was kindly donated to Honeycomb by [@martinjt](https://github.com/martinjt).
