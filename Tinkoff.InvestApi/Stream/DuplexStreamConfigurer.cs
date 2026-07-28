using Grpc.Core;

namespace Tinkoff.InvestApi.Stream;

/// <summary>
/// Конфигуратор для двунаправленного stream-соединения
/// </summary>
public class ResilenceDuplexStreamConfigurer<TClient, TRequest, TResponse>(TClient client, Func<TClient, CallOptions, AsyncDuplexStreamingCall<TRequest, TResponse>> callMethod) : StreamConfigurer<TClient, TRequest, TResponse>(client)
where TRequest : class
where TResponse : class
{
    /// <summary>
    /// Функция вызова метода для запуска stream-соединения
    /// </summary>
    public Func<TClient, CallOptions, AsyncDuplexStreamingCall<TRequest, TResponse>> CallMethod { get; set; } = callMethod;

    /// <summary>
    /// Максимальное время ожидания установки stream-соединения перед отправкой запроса в миллисекундах
    /// </summary>
    public long ConnectTimeoutMs { get; set; } = 30000;

    public ResilenceDuplexStreamConfigurer<TClient, TRequest, TResponse> WithCallMethod(Func<TClient, CallOptions, AsyncDuplexStreamingCall<TRequest, TResponse>> callMethod)
    {
        CallMethod = callMethod;
        return this;
    }

    public ResilenceDuplexStreamConfigurer<TClient, TRequest, TResponse> WithCallOptions(CallOptions callOptions)
    {
        CallOptions = callOptions;
        return this;
    }

    public ResilenceDuplexStreamConfigurer<TClient, TRequest, TResponse> WithBackOffDelayMs(int backOffDelayMs)
    {
        BackOffDelayMs = backOffDelayMs;
        return this;
    }

    public ResilenceDuplexStreamConfigurer<TClient, TRequest, TResponse> WithPingDelayMs(int pingDelayMs)
    {
        PingDelayMs = pingDelayMs;
        return this;
    }

    public ResilenceDuplexStreamConfigurer<TClient, TRequest, TResponse> WithInactivityTimeoutMs(long inactivityTimeoutMs)
    {
        InactivityTimeoutMs = inactivityTimeoutMs;
        return this;
    }

    public ResilenceDuplexStreamConfigurer<TClient, TRequest, TResponse> WithMaxRetryAttampt(int maxRetryAttampt)
    {
        MaxRetryAttampt = maxRetryAttampt;
        return this;
    }

    public ResilenceDuplexStreamConfigurer<TClient, TRequest, TResponse> WithConnectTimeoutMs(long connectTimeoutMs)
    {
        ConnectTimeoutMs = connectTimeoutMs;
        return this;
    }
}