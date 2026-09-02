using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.Stream;
using Tinkoff.InvestApi.Stream.Marketdata;
using Tinkoff.InvestApi.Stream.Marketdata.Models;
using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi.V1.Marketdata;

namespace Tinkoff.InvestApi.Examples.MarketDataStream;

public class Example2(ILogger<Example1> logger, InvestApiClient investClient)
{

    public async Task Run()
    {
        var shares = investClient.Instruments.Shares(new InstrumentsRequest()).Instruments
        .Where(x => x.ApiTradeAvailableFlag && x.TradingStatus.Equals(SecurityTradingStatus.NormalTrading));
        var configurer = new ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(investClient.MarketDataStream, (client, options) => client.MarketDataStream(options));
        configurer.WithPingDelayMs(1000)
        .WithInactivityTimeoutMs(10000);
        var wrapper = new MarketDataStreamWrapper(configurer, new MarketDataStreamContext());
        await wrapper.ConnectAsync();
        await wrapper.SubscribeCandlesAsync(shares.Select(x => new CandleInstrument { Figi = x.Figi, Interval = SubscriptionInterval.OneMinute }), (response) =>
        {
            logger.LogInformation(JsonSerializer.Serialize(response));
        });
    }
}
