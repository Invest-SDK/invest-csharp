using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using Tinkoff.InvestApi.Stream;
using Tinkoff.InvestApi.Stream.Marketdata;
using Tinkoff.InvestApi.V1;
using Xunit;

namespace Tinkoff.InvestApi.Tests.Marketdata;

/// <summary>
/// Тесты для MarketDataStreamManager.
/// Примечание: Полноценные тесты распределения подписок требуют mock gRPC сервера,
/// поэтому здесь тестируются только базовые сценарии инициализации.
/// </summary>
public class MarketDataStreamManagerTests
{
    [Fact]
    public void Constructor_ShouldCreateManagerWithEmptyPool()
    {
        // Arrange
        var configurer = CreateConfigurer();

        // Act
        var manager = new MarketDataStreamManager(
            maxSubscriptionsPerStream: 10,
            maxStreams: 3,
            configurer: configurer);

        // Assert
        manager.Should().NotBeNull();
        manager.Context.Should().NotBeNull();
        manager.StreamCount.Should().Be(0);
        manager.TotalSubscriptionCount.Should().Be(0);
    }

    private static ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse> CreateConfigurer()
    {
        var mockClient = new MockMarketDataStreamClient();
        return new ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(
            mockClient, (c, co) => c.MarketDataStream(co))
            .WithInactivityTimeoutMs(10000)
            .WithPingDelayMs(1000);
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