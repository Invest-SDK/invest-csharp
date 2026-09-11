namespace Tinkoff.InvestApi.V1.Marketdata.Models.Wrapper.Response;

/// <summary>
/// Независимая модель свечи, значения которой инициализируются копированием из grpc-модели.
/// </summary>
public record CandleWrapper
{
    /// <summary>
    /// FIGI-идентификатор инструмента.
    /// </summary>
    public required string Figi { get; init; }

    /// <summary>
    /// Тикер инструмента.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Класс-код инструмента.
    /// </summary>
    public required string ClassCode { get; init; }

    /// <summary>
    /// Интервал свечи.
    /// </summary>
    public required SubscriptionInterval Interval { get; init; }

    /// <summary>
    /// Цена открытия за 1 инструмент.
    /// </summary>
    public required decimal Open { get; init; }

    /// <summary>
    /// Максимальная цена за 1 инструмент.
    /// </summary>
    public required decimal High { get; init; }

    /// <summary>
    /// Минимальная цена за 1 инструмент.
    /// </summary>
    public required decimal Low { get; init; }

    /// <summary>
    /// Цена закрытия за 1 инструмент.
    /// </summary>
    public required decimal Close { get; init; }

    /// <summary>
    /// Объём сделок в лотах.
    /// </summary>
    public required long Volume { get; init; }

    /// <summary>
    /// Объём сделок в лотах на покупку.
    /// </summary>
    public required long VolumeBuy { get; init; }

    /// <summary>
    /// Объём сделок в лотах на продажу.
    /// </summary>
    public required long VolumeSell { get; init; }

    /// <summary>
    /// Время начала интервала свечи по UTC.
    /// </summary>
    public required DateTime? Time { get; init; }

    /// <summary>
    /// Время последней сделки, вошедшей в свечу, по UTC.
    /// </summary>
    public required DateTime? LastTradeTime { get; init; }

    /// <summary>
    /// UID инструмента.
    /// </summary>
    public required string InstrumentUid { get; init; }

    /// <summary>
    /// Тип источника свечей.
    /// </summary>
    public required CandleSource CandleSourceType { get; init; }

    /// <summary>
    /// Признак того, что свеча является исторической (получена при восстановлении подписки).
    /// </summary>
    public required bool IsHistorical { get; init; }
}