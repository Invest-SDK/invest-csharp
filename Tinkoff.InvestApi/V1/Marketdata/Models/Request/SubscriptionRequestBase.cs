namespace Tinkoff.InvestApi.V1.Marketdata.Models.Request;

/// <summary>
/// Базовый класс для запросов подписки на market data
/// </summary>
public abstract class SubscriptionRequestBase(SubscriptionAction action, List<MarketDataInstrument> instruments)
{
    protected readonly SubscriptionAction _action = action;
    protected readonly List<MarketDataInstrument> _instruments = instruments;

    /// <summary>
    /// Действие подписки (Subscribe/Unsubscribe)
    /// </summary>
    public SubscriptionAction Action => _action;

    /// <summary>
    /// Инструменты подписки
    /// </summary>
    public IReadOnlyList<MarketDataInstrument> Instruments => _instruments.AsReadOnly();

    /// <summary>
    /// Преобразует запрос в прото-сообщение MarketDataRequest
    /// </summary>
    public abstract MarketDataRequest ToMarketDataRequest();
}
