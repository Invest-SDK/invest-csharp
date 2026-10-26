namespace Tinkoff.InvestApi.V1.Marketdata.Models;

/// <summary>
/// Активная (успешная) подписка на один инструмент market data.
/// Сообщение об успешности подписки всегда содержит ровно один инструмент,
/// поэтому модель хранит одиночный инструмент, а не список, в отличие от запросов подписки.
/// </summary>
public class ActiveSubscription(MarketDataResponseType responseType, MarketDataInstrument instrument)
{
    /// <summary>
    /// Тип рыночных данных подписки
    /// </summary>
    public MarketDataResponseType ResponseType { get; } = responseType;

    /// <summary>
    /// Инструмент подписки
    /// </summary>
    public MarketDataInstrument Instrument { get; } = instrument;

    /// <summary>
    /// Время последнего взаимодействия. Заполняется для свечей при получении нового бара
    /// и используется для восстановления пропущенных свечей при переподключении.
    /// </summary>
    public DateTimeOffset? LastInteractionTime { get; set; }
}