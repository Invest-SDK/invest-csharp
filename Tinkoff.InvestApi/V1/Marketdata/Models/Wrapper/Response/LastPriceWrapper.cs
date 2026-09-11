namespace Tinkoff.InvestApi.V1.Marketdata.Models.Wrapper.Response;

/// <summary>
/// Независимая модель последней цены, значения которой инициализируются копированием из grpc-модели.
/// </summary>
public record LastPriceWrapper
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
    /// Цена инструмента.
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Время последней цены по UTC.
    /// </summary>
    public required DateTime? Time { get; init; }

    /// <summary>
    /// UID инструмента.
    /// </summary>
    public required string InstrumentUid { get; init; }

    /// <summary>
    /// Тип последней цены.
    /// </summary>
    public required LastPriceType Type { get; init; }
}