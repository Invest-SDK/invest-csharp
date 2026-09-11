namespace Tinkoff.InvestApi.V1.Marketdata.Models.Wrapper.Request;

/// <summary>
/// Базовый класс для запросов подписки на market data
/// </summary>
public abstract class SubscriptionRequestBase
{
    /// <summary>
    /// Действие подписки (Subscribe/Unsubscribe)
    /// </summary>
    public abstract SubscriptionAction Action { get; }

    /// <summary>
    /// Преобразует запрос в прото-сообщение MarketDataRequest
    /// </summary>
    public abstract MarketDataRequest ToMarketDataRequest();
}
