using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using Tinkoff.InvestApi.Stream;
using Tinkoff.InvestApi.Stream.Marketdata;
using Tinkoff.InvestApi.Stream.Marketdata.Models;
using Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;
using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi.V1.Marketdata;
using Xunit;

namespace Tinkoff.InvestApi.Tests.Marketdata;

/// <summary>
/// Тесты для MarketDataStreamWrapper.
/// Примечание: Полноценные интеграционные тесты требуют mock gRPC сервера,
/// поэтому здесь тестируются только базовые сценарии использования.
/// </summary>
public class MarketDataStreamWrapperTests
{
    [Fact]
    [Obsolete]
    public void Constructor_ShouldCreateWrapper()
    {
        // Arrange
        var mockClient = new MockMarketDataStreamClient();
        var configurer = new ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(
            mockClient, (c, co) => c.MarketDataStream(co))
            .WithInactivityTimeoutMs(10000)
            .WithPingDelayMs(1000);

        // Act
        var wrapper = new MarketDataStreamWrapper(configurer);

        // Assert
        wrapper.Should().NotBeNull();
        wrapper.IsConnected().Should().BeFalse("подключение ещё не установлено");
    }

    [Fact]
    [Obsolete]
    public async Task Disconnect_ShouldCompleteWithoutError()
    {
        // Arrange
        var mockClient = new MockMarketDataStreamClient();
        var configurer = new ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(
            mockClient, (c, co) => c.MarketDataStream(co))
            .WithInactivityTimeoutMs(10000)
            .WithPingDelayMs(1000);

        var wrapper = new MarketDataStreamWrapper(configurer);

        // Act & Assert
        await wrapper.DisposeAsync(); // Не должно выбрасывать исключений
    }

    /// <summary>
    /// Mock-клиент для тестирования
    /// </summary>
    private class MockMarketDataStreamClient : MarketDataStreamService.MarketDataStreamServiceClient
    {
        public override AsyncDuplexStreamingCall<MarketDataRequest, MarketDataResponse> MarketDataStream(
            CallOptions options)
        {
            throw new NotImplementedException("Этот тест требует реального gRPC соединения");
        }

        public override AsyncDuplexStreamingCall<MarketDataRequest, MarketDataResponse> MarketDataStream(
            Metadata requestHeaders = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Этот тест требует реального gRPC соединения");
        }
    }
}

