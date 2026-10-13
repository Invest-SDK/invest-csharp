using System.Threading;
using Grpc.Core;
using Tinkoff.InvestApi.Stream;

namespace Tinkoff.InvestApi.Stream;

/// <summary>
/// Класс обёртка для server-side stream-соединения. Выполняет автоматическую проверку состояния соединения и переподключение при необходимости.
/// </summary>
public class ResilienceServerSideStreamWrapper<TClient, TRequest, TResponse>(
    ResilienceServerSideStreamConfigurer<TClient, TRequest, TResponse> streamConfigurer,
    Func<TResponse, Task> messageHandler = null,
    Func<TRequest, Task> connectionHandler = null)
    where TClient : ClientBase<TClient>
    where TRequest : class
    where TResponse : class
{
    private readonly ResilienceServerSideStreamConfigurer<TClient, TRequest, TResponse> _streamConfigurer = streamConfigurer;
    private readonly Func<TResponse, Task> _messageHandler = messageHandler;
    private readonly Func<TRequest, Task> _connectionHandler = connectionHandler;
    private TRequest _request = null!;
    private long _lastInteraction = 0;
    private CancellationTokenSource? _disconnectCts;
    private Task? _healthCheckTask;
    private long _attempts = 0;

    public async Task SubscribeAsync(TRequest request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _disconnectCts = new CancellationTokenSource();

        while (Interlocked.Read(ref _attempts) < _streamConfigurer.MaxRetryAttempt)
        {
            var reconnectCts = new CancellationTokenSource();
            var streamCts = CancellationTokenSource.CreateLinkedTokenSource(_disconnectCts.Token, reconnectCts.Token);

            Interlocked.Exchange(ref _lastInteraction, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            using (var stream = StreamFactory.CreateServerSideStream(_streamConfigurer, _request, streamCts.Token))
            {
                _healthCheckTask = HealthCheckLoopAsync(reconnectCts);
                if (_connectionHandler != null)
                {
                    await _connectionHandler(_request);
                }

                try
                {
                    await ProcessStreamAsync(stream, streamCts.Token);
                    if (_disconnectCts.IsCancellationRequested)
                    {
                        return;
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && !streamCts.IsCancellationRequested)
                {
                    return;
                }
                catch (RpcException)
                {
                    // Ошибка стрима — переподключаемся
                }
                finally
                {
                    streamCts.Cancel();
                    reconnectCts.Cancel();
                }
            }

            if (_healthCheckTask != null)
            {
                try
                {
                    await _healthCheckTask;
                }
                catch (OperationCanceledException)
                {
                    // Нормальное завершение
                }
            }

            Interlocked.Increment(ref _attempts);
            // Backoff перед переподключением
            await Task.Delay(_streamConfigurer.BackOffDelayMs, _disconnectCts.Token);
        }
    }

    private async Task ProcessStreamAsync(AsyncServerStreamingCall<TResponse> stream, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var response = await stream.ResponseStream.MoveNext(cancellationToken);
            if (!response)
            {
                return;
            }

            Interlocked.Exchange(ref _lastInteraction, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            Interlocked.Exchange(ref _attempts, 0);

            if (_messageHandler != null)
            {
                await _messageHandler(stream.ResponseStream.Current);
            }
        }
    }

    private async Task HealthCheckLoopAsync(CancellationTokenSource reconnectCts)
    {
        try
        {
            while (!reconnectCts.IsCancellationRequested)
            {
                await Task.Delay(_streamConfigurer.PingDelayMs);

                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var lastInteraction = Interlocked.Read(ref _lastInteraction);
                var elapsed = now - lastInteraction;

                if (elapsed >= _streamConfigurer.InactivityTimeoutMs)
                {
                    reconnectCts.Cancel();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение
        }
    }

    public void Disconnect()
    {
        _disconnectCts?.Cancel();
    }
}
