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
        var wrapper = new MarketDataStreamWrapper(configurer, new MarketDataStreamContext(), responseHandler: (response) =>
        {
            logger.LogInformation(JsonSerializer.Serialize(response));
            return Task.CompletedTask;
        }, loggerFactory);
        var task = wrapper.ConnectAsync();
        await wrapper.SubscribeCandlesAsync([new CandleInstrument() { Figi = "TCS00A10DT08", Interval = SubscriptionInterval.Month }]);
        await task;
    }
}
