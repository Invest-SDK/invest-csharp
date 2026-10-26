using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi.V1.Marketdata.Models;

namespace Tinkoff.InvestApi.V1.Marketdata;

/// <summary>
/// Единый источник истины для строкового ключа подписки market data.
/// Ключ используется менеджером пула и обёрткой стрима для сопоставления
/// инструментов с активными подписками и маршрутизации отписок.
/// </summary>
internal static class SubscriptionKeyBuilder
{
    /// <summary>
    /// Строит ключ подписки: тип ответа, id инструмента и дискриминатор
    /// (интервал для свечей, тип для стакана; пуст для остальных типов).
    /// </summary>
    public static string Build(MarketDataResponseType responseType, MarketDataInstrument instrument)
    {
        var discriminator = responseType switch
        {
            MarketDataResponseType.CANDLE => instrument.Interval?.ToString() ?? string.Empty,
            MarketDataResponseType.ORDER_BOOK => (instrument.OrderBook ?? OrderBookType.All).ToString(),
            _ => string.Empty
        };
        return $"{responseType}:{instrument.Figi}:{instrument.Uid}:{discriminator}";
    }
}