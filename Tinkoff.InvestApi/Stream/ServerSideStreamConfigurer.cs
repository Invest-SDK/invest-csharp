using Grpc.Core;

namespace Tinkoff.InvestApi.Stream;

/// <summary>
/// Конфигуратор для server-side stream-соединения
/// </summary>
public class ResilenceServerSideStreamConfigurer<TClient, TRequest, TResponse>(TClient client, Func<TClient, TRequest, CallOptions, AsyncServerStreamingCall<TResponse>> callMethod) : StreamConfigurer<TClient, TRequest, TResponse>(client)
where TRequest : class
where TResponse : class
{
    /// <summary>
    /// Функция вызова метода для запуска stream-соединения
    /// </summary>
    public Func<TClient, TRequest, CallOptions, AsyncServerStreamingCall<TResponse>> CallMethod { get; set; } = callMethod;

    public ResilenceServerSideStreamConfigurer<TClient, TRequest, TResponse> WithCallMethod(Func<TClient, TRequest, CallOptions, AsyncServerStreamingCall<TResponse>> callMethod)
    {
        CallMethod = callMethod;
        return this;
    }

    public ResilenceServerSideStreamConfigurer<TClient, TRequest, TResponse> WithCallOptions(CallOptions callOptions)
    {
        CallOptions = callOptions;
        return this;
    }

    public ResilenceServerSideStreamConfigurer<TClient, TRequest, TResponse> WithBackOffDelayMs(int backOffDelayMs)
    {
        BackOffDelayMs = backOffDelayMs;
        return this;
    }

    public ResilenceServerSideStreamConfigurer<TClient, TRequest, TResponse> WithPingDelayMs(int pingDelayMs)
    {
        PingDelayMs = pingDelayMs;
        return this;
    }

    public ResilenceServerSideStreamConfigurer<TClient, TRequest, TResponse> WithInactivityTimeoutMs(long inactivityTimeoutMs)
    {
        InactivityTimeoutMs = inactivityTimeoutMs;
        return this;
    }

    public ResilenceServerSideStreamConfigurer<TClient, TRequest, TResponse> WithMaxRetryAttampt(int maxRetryAttampt)
    {
        MaxRetryAttampt = maxRetryAttampt;
        return this;
    }
}