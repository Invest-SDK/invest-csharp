using Grpc.Core;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream;

/// <summary>
/// Конфигуратор для "MarketDataStreamManager"
/// </summary>
/// <param name="callInvoker">Соединение, из которого строятся стрим- и unary-клиенты</param>
public class MarketDataManagerConfigurer(CallInvoker callInvoker)
: ResilienceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(new MarketDataStreamService.MarketDataStreamServiceClient(callInvoker), (c, options) => c.MarketDataStream(options))
{
    /// <summary>
    /// Клиент unary MarketData, используемый менеджером для восстановления
    /// пропущенных свечей при переподключении стрима. Строится из того же соединения.
    /// </summary>
    public MarketDataService.MarketDataServiceClient MarketData { get; } = new(callInvoker);

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

    public new MarketDataManagerConfigurer WithMaxRetryAttempt(int maxRetryAttempt)
    {
        MaxRetryAttempt = maxRetryAttempt;
        return this;
    }

    public new MarketDataManagerConfigurer WithConnectTimeoutMs(long connectTimeoutMs)
    {
        ConnectTimeoutMs = connectTimeoutMs;
        return this;
    }
}
