using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.Stream;
using Tinkoff.InvestApi.Stream.Marketdata;
using Tinkoff.InvestApi.V1.Marketdata.Models;
using Tinkoff.InvestApi.V1.Marketdata.Models.Response;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Examples.MarketDataStream;

public class Example2(ILogger<Example2> logger, InvestApiClient investClient)
{

    public async Task Run()
    {
        var configurer = new MarketDataManagerConfigurer(investClient.MarketDataStream)
            .WithPingDelayMs(1000)
            .WithInactivityTimeoutMs(10000)
            .WithMaxSubscriptionsPerStream(20);
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Trace);
        });
        var manager = new MarketDataStreamManager(configurer, investClient.MarketData, loggerFactory);

        var instruments = investClient.Instruments.Shares().Instruments
        .Where(x => x.ApiTradeAvailableFlag && x.TradingStatus == SecurityTradingStatus.NormalTrading)
        .Select(x => new MarketDataInstrument(uid: x.Uid, interval: SubscriptionInterval.OneMinute))
        .ToList();
        Func<CandleModel, Task> onCandleHandler = (CandleModel candle) => 
        {
            logger?.LogInformation("Candle: {Candle}", JsonSerializer.Serialize(candle));
            return Task.CompletedTask;
        };
        var _ = manager.StartAsync();
        await manager.SubscribeCandlesAsync(instruments, onCandle: onCandleHandler);
        await Task.Delay(10000);
        await manager.UnsubscribeCandlesAsync(instruments);
        await manager.DisposeAsync();
    }
}
