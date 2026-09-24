using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Tinkoff.InvestApi.Stream;
using Xunit.Abstractions;

namespace Tinkoff.InvestApi.Tests.Integration.Wrapper;

[Collection("GrpcIntegration")]
public class ResilenceDuplexStreamWrapperTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly GrpcTestServerFixture _fixture;
    private readonly ConcurrentBag<string> _receivedMessages;

    public ResilenceDuplexStreamWrapperTests(GrpcTestServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _receivedMessages = new ConcurrentBag<string>();
    }

    public async Task InitializeAsync()
    {
        _fixture.ResetService();
        _receivedMessages.Clear();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _fixture.ResetService();
        await Task.CompletedTask;
    }

    private void Write(string message)
    {
        _output.WriteLine(message);
    }

    [Fact]
    public async Task Call_ShouldReconnect_AfterInactivityTimeout()
    {
        var inactivityTimeoutMs = 500L;
        var pingDelayMs = 100;
        var totalWaitTime = inactivityTimeoutMs * 2 + 200;

        TestStreamServiceImpl.MessagesToSendDuplex = int.MaxValue;
        TestStreamServiceImpl.ShouldSendMessagesDuplex = false;
        TestStreamServiceImpl.ShouldThrowExceptionDuplex = false;
        TestStreamServiceImpl.MessageDelayMsDuplex = pingDelayMs;

        long attempts = 0;
        var reconnectDetected = false;
        var channel = _fixture.CreateChannel();
        var client = new TestStreamService.TestStreamServiceClient(channel);
        var configurer = new ResilenceDuplexStreamConfigurer<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(client, (c, co) => c.DuplexStream(co))
        .WithInactivityTimeoutMs(inactivityTimeoutMs)
        .WithPingDelayMs(pingDelayMs)
        .WithBackOffDelayMs(100);

        var wrapper = new ResilenceDuplexStreamWrapper<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(
            configurer,
            messageHandler: response =>
            {
                _receivedMessages.Add(response.Payload);
                Write($"[TEST] Received: {response.Payload}");
                return Task.CompletedTask;
            },
            connectionHandler: stream =>
            {
                return Task.Run(async () =>
                {
                    Interlocked.Increment(ref attempts);
                    Write($"[TEST] Connection attempt {attempts}");
                    if (Interlocked.Read(ref attempts) > 1)
                    {
                        reconnectDetected = true;
                    }
                });
            }
        );

        var callTask = Task.Run(async () =>
        {
            try
            {
                await wrapper.ConnectAsync();
                var request = new SubscribeRequest { SubscriptionId = Guid.NewGuid().ToString() };
                await wrapper.CallAsync(request);
            }
            catch (Exception ex)
            {
                Write($"[TEST] Exception: {ex.Message}");
            }
        });

        await Task.Delay((int)totalWaitTime);
        wrapper.Disconnect();
        await callTask;

        reconnectDetected.Should().BeTrue(
            $"ожидаем переподключение после {inactivityTimeoutMs}мс без активности. " +
            $"Attempts: {attempts}, Duplex connections: {TestStreamServiceImpl.DuplexConnectionCount}");
    }

    [Fact]
    public async Task Call_ShouldNotReconnect_WhenClientSendsMessages()
    {
        var inactivityTimeoutMs = 500L;
        var pingDelayMs = 100;
        var messageDelayMs = 50;

        TestStreamServiceImpl.MessagesToSendDuplex = 0;
        TestStreamServiceImpl.ShouldSendMessagesDuplex = false;
        TestStreamServiceImpl.ShouldThrowExceptionDuplex = false;
        TestStreamServiceImpl.MessageDelayMsDuplex = messageDelayMs;

        long messagesSentCount = 0;
        long connectionAttempts = 0;
        var channel = _fixture.CreateChannel();
        var client = new TestStreamService.TestStreamServiceClient(channel);
        var configurer = new ResilenceDuplexStreamConfigurer<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(client, (c, co) => c.DuplexStream(co))
        .WithInactivityTimeoutMs(inactivityTimeoutMs)
        .WithPingDelayMs(pingDelayMs)
        .WithBackOffDelayMs(100);

        var wrapper = new ResilenceDuplexStreamWrapper<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(
            configurer,
            connectionHandler: stream =>
            {
                var attempt = Interlocked.Increment(ref connectionAttempts);
                Write($"[TEST] Connection attempt #{attempt}");
                if (attempt > 1)
                {
                    throw new InvalidOperationException($"[TEST] Unexpected reconnection detected! Attempt #{attempt}");
                }
                return Task.CompletedTask;
            }
        );

        var callTask = Task.Run(async () =>
        {
            try
            {
                await wrapper.ConnectAsync();
                while (true)
                {
                    var request = new SubscribeRequest { SubscriptionId = Guid.NewGuid().ToString() };
                    await wrapper.CallAsync(request);
                    Interlocked.Increment(ref messagesSentCount);
                    await Task.Delay(messageDelayMs);
                }
            }
            catch (OperationCanceledException)
            {
                Write("[TEST] Call cancelled");
            }
            catch (Exception ex)
            {
                Write($"[TEST] Exception: {ex.Message}");
            }
        });

        await Task.Delay(300);
        var messagesBeforeWait = Interlocked.Read(ref messagesSentCount);
        var attemptsBeforeWait = Interlocked.Read(ref connectionAttempts);

        Write($"[TEST] Before wait - Messages sent: {messagesBeforeWait}, Attempts: {attemptsBeforeWait}");

        await Task.Delay((int)inactivityTimeoutMs + 200);

        var messagesAfterWait = Interlocked.Read(ref messagesSentCount);
        var attemptsAfterWait = Interlocked.Read(ref connectionAttempts);

        Write($"[TEST] After wait - Messages sent: {messagesAfterWait}, Attempts: {attemptsAfterWait}");

        messagesAfterWait.Should().BeGreaterThan(4, "клиент должен активно отправлять сообщения");
        connectionAttempts.Should().Be(1, "не должно быть переподключений при активной отправке сообщений клиентом");

        wrapper.Disconnect();
        await callTask;
    }

    [Fact]
    public async Task Call_ShouldNotReconnect_WhenServerSendsMessages()
    {
        var inactivityTimeoutMs = 500L;
        var pingDelayMs = 100;
        var messageDelayMs = 50;

        TestStreamServiceImpl.MessagesToSendDuplex = int.MaxValue;
        TestStreamServiceImpl.ShouldSendMessagesDuplex = true;
        TestStreamServiceImpl.ShouldThrowExceptionDuplex = false;
        TestStreamServiceImpl.MessageDelayMsDuplex = messageDelayMs;

        int messagesReceived = 0;
        var connectionCountBeforeWait = 0;
        var connectionCountAfterWait = 0;
        long connectionAttempts = 0;

        var callTask = Task.Run(async () =>
        {
            var channel = _fixture.CreateChannel();
            var client = new TestStreamService.TestStreamServiceClient(channel);
            var configurer = new ResilenceDuplexStreamConfigurer<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(client, (c, co) => c.DuplexStream(co))
            .WithInactivityTimeoutMs(inactivityTimeoutMs)
            .WithPingDelayMs(pingDelayMs)
            .WithBackOffDelayMs(100);

            var wrapper = new ResilenceDuplexStreamWrapper<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(
                configurer,
                messageHandler: response =>
                {
                    _receivedMessages.Add(response.Payload);
                    Interlocked.Increment(ref messagesReceived);
                    Write($"[TEST] Received: {response.Payload} (count: {messagesReceived})");
                    return Task.CompletedTask;
                },
                connectionHandler: stream =>
                {
                    var attempt = Interlocked.Increment(ref connectionAttempts);
                    Write($"[TEST] Connection attempt #{attempt}");
                    if (attempt > 1)
                    {
                        throw new InvalidOperationException($"[TEST] Unexpected reconnection detected! Attempt #{attempt}");
                    }
                    return Task.CompletedTask;
                }
            );

            await wrapper.ConnectAsync();
            await wrapper.CallAsync(new SubscribeRequest { SubscriptionId = Guid.NewGuid().ToString() });
        });

        await Task.Delay(400);

        connectionCountBeforeWait = TestStreamServiceImpl.DuplexConnectionCount;
        var messagesBeforeWait = messagesReceived;

        Write($"[TEST] Before wait - Connections: {connectionCountBeforeWait}, Messages: {messagesBeforeWait}, Attempts: {connectionAttempts}");

        await Task.Delay((int)inactivityTimeoutMs + 200);

        connectionCountAfterWait = TestStreamServiceImpl.DuplexConnectionCount;

        Write($"[TEST] After wait - Connections: {connectionCountAfterWait}, Messages: {messagesReceived}, Attempts: {connectionAttempts}");

        messagesReceived.Should().BeGreaterThan(4, "должны получить хотя бы 5 сообщений");
        connectionAttempts.Should().Be(1, "не должно быть переподключений при активной передаче сообщений со стороны сервера");

        await callTask;
    }

    [Fact]
    public async Task Call_ShouldReconnect_WhenServerThrowsException()
    {
        var inactivityTimeoutMs = 1000L;
        var pingDelayMs = 100;
        long attempts = 0;

        TestStreamServiceImpl.ShouldThrowExceptionDuplex = true;
        TestStreamServiceImpl.ShouldSendMessagesDuplex = false;

        var reconnectAttempted = false;
        var channel = _fixture.CreateChannel();
        var client = new TestStreamService.TestStreamServiceClient(channel);
        var configurer = new ResilenceDuplexStreamConfigurer<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(client, (c, co) => c.DuplexStream(co))
        .WithInactivityTimeoutMs(inactivityTimeoutMs)
        .WithPingDelayMs(pingDelayMs)
        .WithBackOffDelayMs(100)
        .WithMaxRetryAttampt(3);

        var wrapper = new ResilenceDuplexStreamWrapper<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(
            configurer,
            connectionHandler: stream =>
            {
                return Task.Run(async () =>
                {
                    Interlocked.Increment(ref attempts);
                    Write($"[TEST] Connection attempt {attempts}");
                    if (Interlocked.Read(ref attempts) > 1)
                    {
                        reconnectAttempted = true;
                    }
                });
            }
        );

        var callTask = Task.Run(async () =>
        {
            var initialConnectionCount = TestStreamServiceImpl.DuplexConnectionCount;

            try
            {
                await wrapper.ConnectAsync();
                await wrapper.CallAsync(new SubscribeRequest { SubscriptionId = Guid.NewGuid().ToString() });
            }
            catch (Exception ex)
            {
                Write($"[TEST] Exception: {ex.Message}");
            }

            var finalConnectionCount = TestStreamServiceImpl.DuplexConnectionCount;
            Write($"[TEST] Connections: {initialConnectionCount} -> {finalConnectionCount}");
        });

        await Task.Delay(1000);

        reconnectAttempted.Should().BeTrue(
            $"ожидаем переподключение после ошибки сервера. " +
            $"Total connections: {TestStreamServiceImpl.DuplexConnectionCount}");

        wrapper.Disconnect();
        await callTask;
        attempts.Should().Be(configurer.MaxRetryAttampt, "должно быть " + configurer.MaxRetryAttampt + " попыток переподключения");

        Write($"[TEST] Total connections: {TestStreamServiceImpl.DuplexConnectionCount}");
    }

    [Fact]
    public async Task CallAsync_ShouldNotThrowTimeoutException_WhenConnectAsyncWasNeverCalled()
    {
        var channel = _fixture.CreateChannel();
        var client = new TestStreamService.TestStreamServiceClient(channel);
        var configurer = new ResilenceDuplexStreamConfigurer<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(client, (c, co) => c.DuplexStream(co))
        .WithConnectTimeoutMs(200);

        var wrapper = new ResilenceDuplexStreamWrapper<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(configurer);

        var request = new SubscribeRequest { SubscriptionId = Guid.NewGuid().ToString() };

        Func<Task> act = () => wrapper.CallAsync(request);

        await act.Should().NotThrowAsync<TimeoutException>("После запроса должно быть автоматически создано подключение");
    }
}
