using Grpc.Core;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream;

/// <summary>
/// Конфигуратор для stream-соединения
/// </summary>
public abstract class StreamConfigurer<TClient, TRequest, TResponse>(TClient client)
where TRequest : class
where TResponse : class
{

    /// <summary>
    /// Клиент для вызова метода
    /// </summary>
    public TClient Client { get; set; } = client;
    /// <summary>
    /// Опции вызова метода
    /// </summary>
    public CallOptions CallOptions { get; set; } = new CallOptions();
    /// <summary>
    /// Задержка между попытками подключения в миллисекундах
    /// </summary>
    public int BackOffDelayMs { get; set; } = 1000;
    /// <summary>
    /// Задержка между проверками состояния stream-соединения в миллисекундах
    /// </summary>
    public int PingDelayMs { get; set; } = 5000;
    /// <summary>
    /// Максимальное время бездействия в миллисекундах
    /// </summary>
    public long InactivityTimeoutMs { get; set; } = 30000;
    /// <summary>
    /// Максимальное количество попыток подключения
    /// </summary>
    public int MaxRetryAttampt { get; set; } = 5;

    public StreamConfigurer<TClient, TRequest, TResponse> WithClient(TClient client)
    {
        Client = client;
        return this;
    }
}
