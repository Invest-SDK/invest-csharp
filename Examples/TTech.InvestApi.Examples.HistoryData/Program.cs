
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TTech.InvestApi.Examples.HistoryData.Infrastructure;
using TTech.InvestApi.Examples.HistoryData.Usecases;
using TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades;

var date = new DateOnly(2023, 9, 27);
var instrumentIds = new[] { "MASI_SPBXM", "ONTO_SPBXM", "HYDR_SMAL", "STAG_SPBXM" };

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddInvestApiClient((_, settings) =>
{
    settings.AccessToken = ""; // Access token is not required for history data
});
builder.Services.AddInfrastructure();
builder.Services.AddUsecases();
using IHost host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var serviceScopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();
var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

logger.LogInformation("GetHistoryTrades started");
using var source = CancellationTokenSource.CreateLinkedTokenSource(applicationLifetime.ApplicationStopping);
var tasks = instrumentIds.Select(async (instrumentId) =>
{
    await using var scope = serviceScopeFactory.CreateAsyncScope();
    var useCase = scope.ServiceProvider.GetRequiredService<IHistoryTradesUsecase>();
    await useCase.GetHistoryTrades(date, instrumentId, source.Token);
}).ToArray();
await Task.WhenAll(tasks);
logger.LogInformation("GetHistoryTrades completed");

//
// Example to get history minute candles
//
// await using var scope = serviceScopeFactory.CreateAsyncScope();
// var useCase = scope.ServiceProvider.GetRequiredService<IHistoryCandlesUsecase>();
// await useCase.GetHistoryCandles(2020, Guid.Parse("4b7e4dda-6c97-4a91-a6ad-197a86af723e"), CancellationToken.None);
