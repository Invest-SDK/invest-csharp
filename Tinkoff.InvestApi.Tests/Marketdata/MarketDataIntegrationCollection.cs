namespace Tinkoff.InvestApi.Tests.Marketdata;

/// <summary>
/// Определяет коллекцию для интеграционных тестов MarketDataStreamManager.
/// Все тесты коллекции выполняются последовательно и используют один MarketDataGrpcServerFixture.
/// </summary>
[CollectionDefinition("MarketDataIntegration")]
public class MarketDataIntegrationCollection : ICollectionFixture<MarketDataGrpcServerFixture>
{
}