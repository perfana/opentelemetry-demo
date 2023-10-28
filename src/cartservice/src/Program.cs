// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
using System;

using cartservice.cartstore;
using cartservice.featureflags;
using cartservice.services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.ResourceDetectors.Container;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


var builder = WebApplication.CreateBuilder(args);
string redisAddress = builder.Configuration["REDIS_ADDR"];
if (string.IsNullOrEmpty(redisAddress))
{
    Console.WriteLine("REDIS_ADDR environment variable is required.");
    Environment.Exit(1);
}

builder.Logging
    .AddOpenTelemetry(options => options.AddOtlpExporter())
    .AddConsole();

builder.Services.AddSingleton<ICartStore>(x=>
{
    var store = new RedisCartStore(x.GetRequiredService<ILogger<RedisCartStore>>(), redisAddress);
    store.Initialize();
    return store;
});

builder.Services.AddSingleton<FeatureFlagHelper>();
builder.Services.AddSingleton(x => new CartService(x.GetRequiredService<ICartStore>(),
    new RedisCartStore(x.GetRequiredService<ILogger<RedisCartStore>>(), "badhost:1234"),
    x.GetRequiredService<FeatureFlagHelper>()));


// see https://opentelemetry.io/docs/instrumentation/net/getting-started/
private class TraceBaggageEnricher : BaseProcessor<Activity>
{
    public override void OnEnd(Activity data)
    {
        var baggageDictionary = Baggage.GetBaggage();
        foreach (var baggage in baggageDictionary)
        {
            Debug.WriteLine($"{Process.GetCurrentProcess().ProcessName} ENRICHING via Baggage.GetBaggage {baggage.Key}:{baggage.Value}");
            data.SetTag(baggage.Key, baggage.Value);
        }

        foreach(var baggage in data.Baggage)
        {
            Debug.WriteLine($"{Process.GetCurrentProcess().ProcessName} ENRICHING via Activity.Baggage {baggage.Key}:{baggage.Value}");
            data.SetTag(baggage.Key, baggage.Value);
        }
    }
}

Action<ResourceBuilder> appResourceBuilder =
    resource => resource
        .AddDetector(new ContainerResourceDetector());

builder.Services.AddOpenTelemetry()
    .ConfigureResource(appResourceBuilder)
    .WithTracing(tracerBuilder => tracerBuilder
        .AddRedisInstrumentation(
            options => options.SetVerboseDatabaseStatements = true)
        .AddAspNetCoreInstrumentation()
        .AddProcessor(new TraceBaggageEnricher())
        .AddGrpcClientInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(meterBuilder => meterBuilder
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());

builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks()
    .AddCheck("Sample", () => HealthCheckResult.Healthy());

var app = builder.Build();

var redisCartStore = (RedisCartStore) app.Services.GetRequiredService<ICartStore>();
app.Services.GetRequiredService<StackExchangeRedisInstrumentation>().AddConnection(redisCartStore.GetConnection());

app.MapGrpcService<CartService>();
app.MapGrpcHealthChecksService();

app.MapGet("/", async context =>
{
    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
});

app.Run();
