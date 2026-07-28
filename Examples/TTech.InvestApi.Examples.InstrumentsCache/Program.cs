
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi;
using TTech.InvestApi.Examples.InstrumentsCache;

var token = Environment.GetEnvironmentVariable("TOKEN"); // Put your sandbox access token here

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddInvestApiClient((_, settings) =>
{
    settings.AccessToken = token;
    settings.Sandbox = true;
});

builder.Services.AddTransient<Example1>();
builder.Services.AddTransient<Example2>();
builder.Services.AddTransient<Example3>();

using IHost host = builder.Build();

var serviceScopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();
var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
using var source = CancellationTokenSource.CreateLinkedTokenSource(applicationLifetime.ApplicationStopping);
await using var scope = serviceScopeFactory.CreateAsyncScope();
var client = scope.ServiceProvider.GetRequiredService<InvestApiClient>();
var example = scope.ServiceProvider.GetRequiredService<Example1>();
await example.Run(source.Token);
// await host.RunAsync();
