namespace Tinkoff.InvestApi.V1.Marketdata.Models.Wrapper.Response;

/// <summary>
/// Независимая модель сделки, значения которой инициализируются копированием из grpc-модели.
/// </summary>
public record TradeWrapper
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
    /// Направление сделки.
    /// </summary>
    public required TradeDirection Direction { get; init; }

    /// <summary>
    /// Цена сделки.
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Количество лотов.
    /// </summary>
    public required long Quantity { get; init; }

    /// <summary>
    /// Время сделки по UTC.
    /// </summary>
    public required DateTime? Time { get; init; }

    /// <summary>
    /// UID инструмента.
    /// </summary>
    public required string InstrumentUid { get; init; }

    /// <summary>
    /// Тип источника сделок.
    /// </summary>
    public required TradeSourceType TradeSource { get; init; }
}