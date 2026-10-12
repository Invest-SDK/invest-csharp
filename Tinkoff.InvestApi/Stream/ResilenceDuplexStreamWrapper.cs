using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Google.Api;
using Grpc.Core;

namespace Tinkoff.InvestApi.Stream;

/// <summary>
/// Класс обёртка для duplex stream-соединения. Выполняет автоматическую проверку состояния соединения и переподключение при необходимости.
/// </summary>
public class ResilenceDuplexStreamWrapper<TClient, TRequest, TResponse>(
    ResilenceDuplexStreamConfigurer<TClient, TRequest, TResponse> streamConfigurer,
    Func<TResponse, Task>? messageHandler = null,
    Func<AsyncDuplexStreamingCall<TRequest, TResponse>, Task>? connectionHandler = null,
    ILogger? logger = null)
    where TRequest : class
    where TResponse : class
    where TClient : ClientBase<TClient>
{
    private readonly Guid streamGuid = Guid.NewGuid();
    private readonly ResilenceDuplexStreamConfigurer<TClient, TRequest, TResponse> _streamConfigurer = streamConfigurer;
    private readonly Func<TResponse, Task>? _messageHandler = messageHandler;
    private readonly Func<AsyncDuplexStreamingCall<TRequest, TResponse>, Task>? _connectionHandler = connectionHandler;
    private readonly ILogger? _logger = logger;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly Channel<QueueItem> _queue = Channel.CreateUnbounded<QueueItem>(new UnboundedChannelOptions { SingleReader = true });
    private readonly CancellationTokenSource _lifetimeCts = new();
    private volatile TaskCompletionSource<bool> _connectedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long _lastInteraction = 0;
    private CancellationTokenSource? _disconnectCts;
    private CancellationTokenSource? _backgroundCts;
    private Task? _readTask;
    private Task? _healthCheckTask;
    private AsyncDuplexStreamingCall<TRequest, TResponse>? _currentStream;
    private volatile bool _isConnecting;
    private long _attampts = 0;
    private int _writeLoopStarted;

    private sealed class QueueItem
    {
        public QueueItem(TRequest request, TaskCompletionSource<bool> completionSource, CancellationToken cancellationToken)
        {
            Request = request;
            CompletionSource = completionSource;
            CancellationToken = cancellationToken;
        }

        public TRequest Request { get; }

        public TaskCompletionSource<bool> CompletionSource { get; }

        public CancellationToken CancellationToken { get; }
    }

    /// <summary>
    /// Устанавливает duplex stream-соединение, если оно ещё не установлено. Не связано с отправкой запросов —
    /// вызывающий код может установить соединение заранее, без вызова <see cref="CallAsync"/>.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_currentStream != null && !_isConnecting)
            {
                return;
            }

            StartBackgroundConnection(cancellationToken);
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async Task CallAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        _ = request ?? throw new ArgumentNullException(nameof(request));

        if (Interlocked.CompareExchange(ref _writeLoopStarted, 1, 0) == 0)
        {
            _ = RunWriteLoopAsync();
        }
        if (!IsConnected())
        {
            _ = ConnectAsync(cancellationToken);
        }

        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await _queue.Writer.WriteAsync(new QueueItem(request, completionSource, cancellationToken), cancellationToken);

        cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));
        await completionSource.Task;
    }

    public bool IsConnected()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var lastInteraction = Interlocked.Read(ref _lastInteraction);
        var elapsed = now - lastInteraction;

        return elapsed < _streamConfigurer.InactivityTimeoutMs && _currentStream != null;
    }

    public void Disconnect()
    {
        _backgroundCts?.Cancel();
        _disconnectCts?.Cancel();
        _lifetimeCts.Cancel();
        _queue.Writer.TryComplete();
    }

    public Guid StreamGuid
    {
        get
        {
            return streamGuid;
        }
    }

    private async Task RunWriteLoopAsync()
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync(_lifetimeCts.Token))
            {
                while (_queue.Reader.TryRead(out var item))
                {
                    if (item.CancellationToken.IsCancellationRequested)
                    {
                        item.CompletionSource.TrySetCanceled(item.CancellationToken);
                        continue;
                    }

                    try
                    {
                        await WaitUntilConnectedAsync(item.CancellationToken);

                        var stream = _currentStream ?? throw new InvalidOperationException("Duplex stream is not connected.");

                        await stream.RequestStream.WriteAsync(item.Request);
                        Interlocked.Exchange(ref _lastInteraction, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                        item.CompletionSource.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        item.CompletionSource.TrySetException(ex);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение (Disconnect/lifetime cancelled)
        }
        finally
        {
            while (_queue.Reader.TryRead(out var pending))
            {
                pending.CompletionSource.TrySetCanceled();
            }
        }
    }

    private void StartBackgroundConnection(CancellationToken cancellationToken)
    {
        _isConnecting = true;
        _disconnectCts = new CancellationTokenSource();
        _backgroundCts = new CancellationTokenSource();

        _ = RunBackgroundLoopAsync(_backgroundCts, cancellationToken);
    }

    private async Task WaitUntilConnectedAsync(CancellationToken cancellationToken)
    {
        if (_currentStream != null && !_isConnecting)
        {
            return;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(_streamConfigurer.ConnectTimeoutMs));

        try
        {
            while (_currentStream == null || _isConnecting)
            {
                var signal = _connectedSignal;
                using var registration = timeoutCts.Token.Register(() => signal.TrySetCanceled(timeoutCts.Token));
                await signal.Task;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Duplex stream did not connect within {_streamConfigurer.ConnectTimeoutMs} ms. Call ConnectAsync before sending.");
        }
    }

    private async Task RunBackgroundLoopAsync(CancellationTokenSource backgroundCts, CancellationToken cancellationToken)
    {
        while (!backgroundCts.IsCancellationRequested && Interlocked.Read(ref _attampts) < _streamConfigurer.MaxRetryAttampt)
        {
            var reconnectCts = new CancellationTokenSource();
            var streamCts = CancellationTokenSource.CreateLinkedTokenSource(_disconnectCts!.Token, reconnectCts.Token, cancellationToken);

            try
            {
                _currentStream = StreamFactory.CreateDuplexStream(_streamConfigurer, streamCts.Token);

                using (_currentStream)
                {
                    Interlocked.Exchange(ref _lastInteraction, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    _healthCheckTask = HealthCheckLoopAsync(reconnectCts);
                    _logger?.LogDebug("Stream {} connected", streamGuid);
                    if (_connectionHandler != null)
                    {
                        await _connectionHandler(_currentStream);
                    }

                    _readTask = ProcessStreamAsync(_currentStream, reconnectCts, streamCts.Token);

                    _isConnecting = false;
                    _connectedSignal.TrySetResult(true);

                    await Task.WhenAny(_readTask, _healthCheckTask);

                    if (_disconnectCts.IsCancellationRequested)
                    {
                        return;
                    }
                }
                _currentStream = null;
                _isConnecting = true;
                _connectedSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            catch (OperationCanceledException) when (!_disconnectCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Healthcheck инициировал реконнект
                _logger?.LogDebug("Required reconnection for stream: {}", streamGuid);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && _disconnectCts.IsCancellationRequested)
            {
                // Нормальное завершение
                _logger?.LogDebug("Stream {} disconnected", streamGuid);
                return;
            }
            catch (RpcException ex)
            {
                // Ошибка стрима — переподключаемся
                _logger?.LogDebug("Exception {} in stream {}. Reconnection...", ex.StatusCode, streamGuid);
            }
            finally
            {
                streamCts.Cancel();
                reconnectCts.Cancel();
            }

            Interlocked.Increment(ref _attampts);
            // Backoff перед переподключением
            await Task.Delay(_streamConfigurer.BackOffDelayMs, backgroundCts.Token);
        }
    }

    private async Task ProcessStreamAsync(AsyncDuplexStreamingCall<TRequest, TResponse> stream, CancellationTokenSource reconnectCts, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var hasResponse = await stream.ResponseStream.MoveNext(cancellationToken);
                    if (!hasResponse)
                    {
                        return;
                    }

                    Interlocked.Exchange(ref _lastInteraction, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    Interlocked.Exchange(ref _attampts, 0);

                    if (_messageHandler != null)
                    {
                        await _messageHandler(stream.ResponseStream.Current);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Нормальное завершение
                    return;
                }
            }
        }
        catch
        {
            // Ошибка чтения — инициируем переподключение
            reconnectCts.Cancel();
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
                    _logger?.LogDebug("Inactivity timeout exceeded for stream: {}", streamGuid);
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

    public async ValueTask DisposeAsync()
    {
        Disconnect();
        _connectLock?.Dispose();
        _lifetimeCts?.Dispose();
        _disconnectCts?.Dispose();
        _backgroundCts?.Dispose();

        if (_currentStream != null)
        {
            try
            {
                await _currentStream.RequestStream.CompleteAsync();
            }
            catch
            {
                // Игнорируем ошибки при завершении стрима
            }
        }
    }
}
