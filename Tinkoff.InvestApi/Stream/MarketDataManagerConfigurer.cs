using Grpc.Core;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream;

/// <summary>
/// Конфигуратор для "MarketDataStreamManager"
/// </summary>
/// <param name="client">Клиент для создания соединения</param>
public class MarketDataManagerConfigurer(MarketDataStreamService.MarketDataStreamServiceClient client) 
: ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(client, (c, options) => c.MarketDataStream(options))
{
    public int MaxSubscriptionsPerStream { get; set; } = 300;
    public int MaxStreams { get; set; } = 16;

    public MarketDataManagerConfigurer WithMaxSubscriptionsPerStream(int maxSubscriptionsPerStream)
    {
        MaxSubscriptionsPerStream = maxSubscriptionsPerStream;
        return this;
    }

    public MarketDataManagerConfigurer WithMaxStreams(int maxStreams)
    {
        MaxStreams = maxStreams;
        return this;
    }

    public new MarketDataManagerConfigurer WithCallOptions(CallOptions callOptions)
    {
        CallOptions = callOptions;
        return this;
    }

    public new MarketDataManagerConfigurer WithBackOffDelayMs(int backOffDelayMs)
    {
        BackOffDelayMs = backOffDelayMs;
        return this;
    }

    public new MarketDataManagerConfigurer WithPingDelayMs(int pingDelayMs)
    {
        PingDelayMs = pingDelayMs;
        return this;
    }

    public new MarketDataManagerConfigurer WithInactivityTimeoutMs(long inactivityTimeoutMs)
    {
        InactivityTimeoutMs = inactivityTimeoutMs;
        return this;
    }

    public new MarketDataManagerConfigurer WithMaxRetryAttampt(int maxRetryAttampt)
    {
        MaxRetryAttampt = maxRetryAttampt;
        return this;
    }

    public new MarketDataManagerConfigurer WithConnectTimeoutMs(long connectTimeoutMs)
    {
        ConnectTimeoutMs = connectTimeoutMs;
        return this;
    }
}
