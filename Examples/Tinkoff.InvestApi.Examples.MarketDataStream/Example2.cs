using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.Stream.Marketdata;
using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi.V1.Marketdata.Models;
using Tinkoff.InvestApi.V1.Marketdata.Models.Response;

namespace Tinkoff.InvestApi.Examples.MarketDataStream;

public class Example2(ILogger<Example2> logger, InvestApiClient investClient, MarketDataStreamManager manager)
{
    public async Task Run()
    {
        var instruments = investClient.Instruments.Shares().Instruments
            .Where(x => x.ApiTradeAvailableFlag && x.TradingStatus == SecurityTradingStatus.NormalTrading)
            .Select(x => new MarketDataInstrument(Uid: x.Uid, Interval: SubscriptionInterval.OneMinute))
            .ToList();

        Func<CandleModel, Task> onCandleHandler = candle =>
        {
            logger.LogInformation("Candle: {Candle}", JsonSerializer.Serialize(candle));
            return Task.CompletedTask;
        };

        var _ = manager.StartAsync();
        await manager.SubscribeCandlesAsync(instruments, onCandle: onCandleHandler);
        await Task.Delay(10000);
        await manager.UnsubscribeCandlesAsync(instruments);
        await manager.DisposeAsync();
    }
}