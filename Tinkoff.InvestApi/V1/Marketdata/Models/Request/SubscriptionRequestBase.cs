namespace Tinkoff.InvestApi.V1.Marketdata.Models.Request;

/// <summary>
/// Базовый класс для запросов подписки на market data
/// </summary>
public interface ISubscriptionRequestBase
{
    /// <summary>
    /// Действие подписки (Subscribe/Unsubscribe)
    /// </summary>
    SubscriptionAction Action { get; }

    /// <summary>
    /// Преобразует запрос в прото-сообщение MarketDataRequest
    /// </summary>
    MarketDataRequest ToMarketDataRequest();
}
