using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Tinkoff.InvestApi.Stream;
using Xunit.Abstractions;

namespace Tinkoff.InvestApi.Tests.Integration.Wrapper;

[Collection("GrpcIntegration")]
public class ResilienceServerSideStreamWrapperTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly GrpcTestServerFixture _fixture;
    private readonly ConcurrentBag<string> _receivedMessages;

    public ResilienceServerSideStreamWrapperTests(GrpcTestServerFixture fixture, ITestOutputHelper output)
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
    public async Task Subscribe_ShouldReconnect_AfterInactivityTimeout()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var inactivityTimeoutMs = 500L;
        var pingDelayMs = 100;
        var totalWaitTime = inactivityTimeoutMs * 2 + 200;

        TestStreamServiceImpl.MessagesToSend = int.MaxValue;
        TestStreamServiceImpl.ShouldSendMessages = false; // Не отправляем сообщения для триггера таймаута
        TestStreamServiceImpl.ShouldThrowException = false;

        long attempts = 0;
        var reconnectDetected = false;
        var channel = _fixture.CreateChannel();
        var client = new TestStreamService.TestStreamServiceClient(channel);
        var configurer = new ResilienceServerSideStreamConfigurer<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(client, (c, req, co) => c.Subscribe(req, co))
        .WithInactivityTimeoutMs(inactivityTimeoutMs)
        .WithPingDelayMs(pingDelayMs)
        .WithBackOffDelayMs(100);

        var wrapper = new ResilienceServerSideStreamWrapper<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(
            streamConfigurer: configurer,
            messageHandler: response =>
            {
                _receivedMessages.Add(response.Payload);
                Write($"[TEST] Received: {response.Payload}");
                return Task.CompletedTask;
            },
            connectionHandler: request =>
            {
                return Task.Run(async () =>
                {
                    Interlocked.Increment(ref attempts);
                    Write($"[TEST] Reconnection attempt {attempts}");
                    if (Interlocked.Read(ref attempts) > 1)
                    {
                        reconnectDetected = true;
                    }
                });
            }
        );

        var subscribeTask = Task.Run(async () =>
        {
            try
            {
                await wrapper.SubscribeAsync(new SubscribeRequest { SubscriptionId = subscriptionId });
            }
            catch (Exception ex)
            {
                Write($"[TEST] Exception: {ex.Message}");
            }
        });

        await Task.Delay((int)totalWaitTime);
        wrapper.Disconnect();
        await subscribeTask;

        reconnectDetected.Should().BeTrue($"ожидаем переподключение после {inactivityTimeoutMs}мс без активности. ");
    }

    [Fact]
    public async Task Subscribe_ShouldNotReconnect_WhenMessagesReceived_AndDisconnectWorks()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var inactivityTimeoutMs = 500L;
        var pingDelayMs = 100;
        var messageDelayMs = 50; // Сообщения чаще чем таймаут

        TestStreamServiceImpl.MessagesToSend = int.MaxValue;
        TestStreamServiceImpl.ShouldSendMessages = true;
        TestStreamServiceImpl.ShouldThrowException = false;
        TestStreamServiceImpl.MessageDelayMs = messageDelayMs;

        int messagesReceivedCount = 0;
        var connectionCountBeforeDisconnect = 0;
        var connectionCountAfterWait = 0;

        var subscribeTask = Task.Run(async () =>
        {
            var channel = _fixture.CreateChannel();
            var client = new TestStreamService.TestStreamServiceClient(channel);
            var configurer = new ResilienceServerSideStreamConfigurer<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(client, (c, req, co) => c.Subscribe(req, co))
            .WithInactivityTimeoutMs(inactivityTimeoutMs)
            .WithPingDelayMs(pingDelayMs)
            .WithBackOffDelayMs(100);

            var wrapper = new ResilienceServerSideStreamWrapper<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(
                configurer,
                response =>
                {
                    _receivedMessages.Add(response.Payload);
                    var count = Interlocked.Increment(ref messagesReceivedCount);
                    Write($"[TEST] Received: {response.Payload} (count: {count})");
                    return Task.CompletedTask;
                }
            );

            await wrapper.SubscribeAsync(
                new SubscribeRequest { SubscriptionId = subscriptionId });
        });

        await Task.Delay(400);

        connectionCountBeforeDisconnect = TestStreamServiceImpl.ConnectionCount;
        var messagesCountBeforeDisconnect = messagesReceivedCount;

        Write($"[TEST] Before disconnect - Connections: {connectionCountBeforeDisconnect}, Messages: {messagesCountBeforeDisconnect}");

        await Task.Delay((int)inactivityTimeoutMs + 200);

        connectionCountAfterWait = TestStreamServiceImpl.ConnectionCount;

        Write($"[TEST] After wait - Connections: {connectionCountAfterWait}, Messages: {messagesCountBeforeDisconnect}");

        messagesCountBeforeDisconnect.Should().BeGreaterThan(4, "должны получить хотя бы 5 сообщений");
        connectionCountBeforeDisconnect.Should().Be(connectionCountAfterWait,
            "не должно быть переподключений при активной передаче сообщений");
    }

    [Fact]
    public async Task Subscribe_ShouldReconnect_AfterStreamClosed()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var inactivityTimeoutMs = 1000L;
        var pingDelayMs = 100;

        TestStreamServiceImpl.MessagesToSend = 3; // Только 3 сообщения, потом ошибка
        TestStreamServiceImpl.ShouldSendMessages = true;
        TestStreamServiceImpl.ShouldThrowException = false;
        TestStreamServiceImpl.MessageDelayMs = 50;

        int messagesReceived = 0;
        long attempts = 0;
        var reconnectDetected = false;

        var subscribeTask = Task.Run(async () =>
        {
            var channel = _fixture.CreateChannel();
            var client = new TestStreamService.TestStreamServiceClient(channel);
            var configurer = new ResilienceServerSideStreamConfigurer<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(client, (c, req, co) => c.Subscribe(req, co))
            .WithInactivityTimeoutMs(inactivityTimeoutMs)
            .WithPingDelayMs(pingDelayMs)
            .WithBackOffDelayMs(100);

            var wrapper = new ResilienceServerSideStreamWrapper<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(
                configurer,
                response =>
                {
                    _receivedMessages.Add(response.Payload);
                    Interlocked.Increment(ref messagesReceived);
                    Write($"[TEST] Received: {response.Payload}");
                    return Task.CompletedTask;
                },
                connectionHandler: request =>
                {
                    return Task.Run(async () =>
                    {
                        Interlocked.Increment(ref attempts);
                        Write($"[TEST] Reconnection attempt {attempts}");
                        if (Interlocked.Read(ref attempts) > 1)
                        {
                            reconnectDetected = true;
                        }
                    });
                }
            );

            await wrapper.SubscribeAsync(
                new SubscribeRequest { SubscriptionId = subscriptionId });
        });

        await Task.Delay(800);

        messagesReceived.Should().BeGreaterThan(2, "должны получить хотя бы 3 сообщения");
        reconnectDetected.Should().BeTrue(
            $"ожидаем переподключение после закрытия стрима. " +
            $"Messages received: {messagesReceived}, Total connections: {TestStreamServiceImpl.ConnectionCount}");

        Write($"[TEST] Total messages: {messagesReceived}, Total connections: {TestStreamServiceImpl.ConnectionCount}");
    }

    [Fact]
    public async Task Subscribe_ShouldReconnect_WhenServerThrowsException()
    {
        var subscriptionId = Guid.NewGuid().ToString();
        var inactivityTimeoutMs = 1000L;
        var pingDelayMs = 100;
        long attempts = 0;

        TestStreamServiceImpl.ShouldThrowException = true; // Сервер бросает исключение
        TestStreamServiceImpl.ShouldSendMessages = false;

        var reconnectAttempted = false;
        var channel = _fixture.CreateChannel();
        var client = new TestStreamService.TestStreamServiceClient(channel);
        var configurer = new ResilienceServerSideStreamConfigurer<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(client, (c, req, co) => c.Subscribe(req, co))
        .WithInactivityTimeoutMs(inactivityTimeoutMs)
        .WithPingDelayMs(pingDelayMs)
        .WithBackOffDelayMs(100)
        .WithMaxRetryAttempt(3);

        var wrapper = new ResilienceServerSideStreamWrapper<TestStreamService.TestStreamServiceClient, SubscribeRequest, StreamMessage>(
            configurer,
            connectionHandler: request =>
            {
                return Task.Run(async () =>
                {
                    Interlocked.Increment(ref attempts);
                    Write($"[TEST] Reconnection attempt {attempts}");
                    if (Interlocked.Read(ref attempts) > 1)
                    {
                        reconnectAttempted = true;
                    }
                });
            }
        );

        var subscribeTask = Task.Run(async () =>
        {
            var initialConnectionCount = TestStreamServiceImpl.ConnectionCount;

            try
            {
                await wrapper.SubscribeAsync(
                    new SubscribeRequest { SubscriptionId = subscriptionId });
            }
            catch (Exception ex)
            {
                Write($"[TEST] Exception: {ex.Message}");
            }

            var finalConnectionCount = TestStreamServiceImpl.ConnectionCount;
            Write($"[TEST] Connections: {initialConnectionCount} -> {finalConnectionCount}");
        });

        await Task.Delay(1000);

        reconnectAttempted.Should().BeTrue(
            $"ожидаем переподключение после ошибки сервера. " +
            $"Total connections: {TestStreamServiceImpl.ConnectionCount}");
        wrapper.Disconnect();
        await subscribeTask;
        attempts.Should().Be(configurer.MaxRetryAttempt, "должно быть " + configurer.MaxRetryAttempt + " попыток переподключения");

        Write($"[TEST] Total connections: {TestStreamServiceImpl.ConnectionCount}");
    }
}
