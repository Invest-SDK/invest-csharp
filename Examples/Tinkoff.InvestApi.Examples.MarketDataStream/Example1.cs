using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.Stream;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Examples.MarketDataStream;

public class Example1(ILogger<Example1> logger, InvestApiClient client)
{
    [Obsolete]
    public async Task Run()
    {
        var configuration = new ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(
            client.MarketDataStream,
            (client, options) => client.MarketDataStream(options))
            .WithPingDelayMs(1000)
            .WithInactivityTimeoutMs(10000);

        var subscribeCandlesRequest = new MarketDataRequest
        {
            SubscribeCandlesRequest = new SubscribeCandlesRequest
            {
                Instruments =
        {
            new CandleInstrument
            {
                Figi = "BBG004730N88",
                Interval = SubscriptionInterval.OneMinute
            }
        },
                SubscriptionAction = SubscriptionAction.Subscribe
            }
        };

        var pingSettingsRequest = new MarketDataRequest
        {
            PingSettings = new PingDelaySettings
            {
                PingDelayMs = 5000
            }
        };
        int i = 0;
        var stream = new ResilenceDuplexStreamWrapper<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(
            configuration,
            async response =>
        {
            logger.LogInformation(JsonSerializer.Serialize(response));
            Interlocked.Increment(ref i);
        });

        await stream.ConnectAsync();
        await stream.CallAsync(subscribeCandlesRequest);
        await stream.CallAsync(pingSettingsRequest);
        await Task.Run(() =>
        {
            while (true)
            {
                if (i == 10)
                {
                    stream.Disconnect();
                    break;
                }
            }
        });
    }
}
