using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Tinkoff.InvestApi.Tests.Integration.Wrapper;

public class TestStreamServiceImpl : TestStreamService.TestStreamServiceBase
{
    private readonly ILogger<TestStreamServiceImpl> _logger;
    private readonly ITestOutputHelper? _output;

    // Статические поля для управления поведением сервиса в тестах
    public static bool ShouldThrowException { get; set; } = false;
    public static bool ShouldSendMessages { get; set; } = true;
    public static int MessagesToSend { get; set; } = int.MaxValue;
    public static int MessageDelayMs { get; set; } = 100;

    // Поля для duplex стрима
    public static bool ShouldThrowExceptionDuplex { get; set; } = false;
    public static bool ShouldSendMessagesDuplex { get; set; } = true;
    public static int MessagesToSendDuplex { get; set; } = int.MaxValue;
    public static int MessageDelayMsDuplex { get; set; } = 100;

    private static int _messagesSentCount = 0;
    private static int _connectionCount = 0;
    private static int _duplexMessagesSentCount = 0;
    private static int _duplexConnectionCount = 0;

    public static int MessagesSentCount => _messagesSentCount;
    public static int ConnectionCount => _connectionCount;
    public static int DuplexMessagesSentCount => _duplexMessagesSentCount;
    public static int DuplexConnectionCount => _duplexConnectionCount;

    public static void Reset()
    {
        ShouldThrowException = false;
        ShouldSendMessages = true;
        MessagesToSend = int.MaxValue;
        MessageDelayMs = 100;
        _messagesSentCount = 0;
        _connectionCount = 0;

        ShouldThrowExceptionDuplex = false;
        ShouldSendMessagesDuplex = true;
        MessagesToSendDuplex = int.MaxValue;
        MessageDelayMsDuplex = 100;
        _duplexMessagesSentCount = 0;
        _duplexConnectionCount = 0;
    }

    public TestStreamServiceImpl(ILogger<TestStreamServiceImpl> logger, ITestOutputHelper? output = null)
    {
        _logger = logger;
        _output = output;
    }

    public override async Task Subscribe(
        SubscribeRequest request,
        IServerStreamWriter<StreamMessage> responseStream,
        ServerCallContext context)
    {
        var connCount = Interlocked.Increment(ref _connectionCount);

        Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] New connection: {request.SubscriptionId}, Total connections: {connCount}");

        var messagesSent = 0;

        try
        {
            if (ShouldThrowException)
            {
                Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Throwing exception for {request.SubscriptionId}");
                throw new RpcException(new Status(StatusCode.Unavailable, "Service unavailable"));
            }

            while (!context.CancellationToken.IsCancellationRequested && messagesSent < MessagesToSend)
            {
                if (!ShouldSendMessages)
                {
                    Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Stopping messages for {request.SubscriptionId}");
                    break;
                }

                var message = new StreamMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Payload = $"Message {messagesSent + 1} for {request.SubscriptionId}"
                };

                await responseStream.WriteAsync(message);
                messagesSent++;
                var msgCount = Interlocked.Increment(ref _messagesSentCount);

                Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Sent message {messagesSent} to {request.SubscriptionId} (total: {msgCount})");

                if (messagesSent < MessagesToSend)
                {
                    await Task.Delay(MessageDelayMs, context.CancellationToken);
                }
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Connection cancelled for {request.SubscriptionId}");
        }
        catch (OperationCanceledException)
        {
            Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Operation cancelled for {request.SubscriptionId}");
        }
        catch (Exception ex)
        {
            Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Error: {ex.Message}");
            throw;
        }

        Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Connection ended for {request.SubscriptionId}. Messages sent: {messagesSent}");
    }

    public override async Task DuplexStream(
        IAsyncStreamReader<SubscribeRequest> requestStream,
        IServerStreamWriter<StreamMessage> responseStream,
        ServerCallContext context)
    {
        var connCount = Interlocked.Increment(ref _duplexConnectionCount);

        Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] New duplex connection, Total connections: {connCount}");

        var messagesSent = 0;

        try
        {
            if (ShouldThrowExceptionDuplex)
            {
                Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Throwing exception for duplex stream");
                throw new RpcException(new Status(StatusCode.Unavailable, "Service unavailable"));
            }

            // Запускаем задачу для чтения входящих сообщений
            var readTask = Task.Run(async () =>
            {
                try
                {
                    while (await requestStream.MoveNext(context.CancellationToken))
                    {
                        var request = requestStream.Current;
                        Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Received request: {request?.SubscriptionId ?? "null"}");
                    }
                }
                catch (OperationCanceledException)
                {
                    Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Read stream cancelled");
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Read stream cancelled (RPC)");
                }
            });

            // Отправляем сообщения клиенту
            while (!context.CancellationToken.IsCancellationRequested && messagesSent < MessagesToSendDuplex)
            {
                if (!ShouldSendMessagesDuplex)
                {
                    Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Stopping messages for duplex stream");
                    break;
                }

                var message = new StreamMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Payload = $"Duplex message {messagesSent + 1}"
                };

                await responseStream.WriteAsync(message);
                messagesSent++;
                var msgCount = Interlocked.Increment(ref _duplexMessagesSentCount);

                Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Sent duplex message {messagesSent} (total: {msgCount})");

                if (messagesSent < MessagesToSendDuplex)
                {
                    await Task.Delay(MessageDelayMsDuplex, context.CancellationToken);
                }
            }

            await readTask;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Duplex connection cancelled");
        }
        catch (OperationCanceledException)
        {
            Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Duplex operation cancelled");
        }
        catch (Exception ex)
        {
            Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Duplex error: {ex.Message}");
            throw;
        }

        Write($"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] Duplex connection ended. Messages sent: {messagesSent}");
    }

    private void Write(string message)
    {
        _output?.WriteLine(message);
        _logger.LogInformation(message);
    }
}
