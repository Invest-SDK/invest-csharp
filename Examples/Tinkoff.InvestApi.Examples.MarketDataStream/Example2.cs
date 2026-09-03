using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.Stream;
using Tinkoff.InvestApi.Stream.Marketdata;
using Tinkoff.InvestApi.Stream.Marketdata.Models;
using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi.V1.Marketdata;

namespace Tinkoff.InvestApi.Examples.MarketDataStream;

public class Example2(ILogger<Example2> logger, InvestApiClient investClient)
{
    [Obsolete]
    public async Task Run()
    {
        var configurer = new ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(investClient.MarketDataStream, (client, options) => client.MarketDataStream(options));
        configurer.WithPingDelayMs(1000)
        .WithInactivityTimeoutMs(10000);
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Trace);
        });
        var manager = new MarketDataStreamManager(1, 3, configurer, loggerFactory);
        manager.Context.OnCandle((response) =>
        {
            logger.LogInformation(JsonSerializer.Serialize(response.Response));
            return Task.CompletedTask;
        });
        var task = manager.StartAsync();
        var sub1 = manager.SubscribeCandlesAsync([new CandleInstrument() { Figi = "TCS00A10DT08", Interval = SubscriptionInterval.Month }]);
        var sub2 = manager.SubscribeCandlesAsync([new CandleInstrument() { Figi = "BBG004730N88", Interval = SubscriptionInterval.Month }]);
        await sub2;
    }
}
