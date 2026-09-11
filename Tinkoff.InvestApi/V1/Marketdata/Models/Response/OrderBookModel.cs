namespace Tinkoff.InvestApi.V1.Marketdata.Models.Response;

/// <summary>
/// Независимая модель записи стакана, значения которой инициализируются копированием из grpc-модели.
/// </summary>
public record OrderModel
{
    /// <summary>
    /// Цена записи.
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Количество лотов.
    /// </summary>
    public required long Quantity { get; init; }
}

/// <summary>
/// Независимая модель стакана, значения которой инициализируются копированием из grpc-модели.
/// </summary>
public record OrderBookModel
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
    /// Глубина стакана.
    /// </summary>
    public required int Depth { get; init; }

    /// <summary>
    /// Флаг консистентности стакана.
    /// </summary>
    public required bool IsConsistent { get; init; }

    /// <summary>
    /// Время последнего обновления стакана по UTC.
    /// </summary>
    public required DateTime? Time { get; init; }

    /// <summary>
    /// UID инструмента.
    /// </summary>
    public required string InstrumentUid { get; init; }

    /// <summary>
    /// Тип стакана.
    /// </summary>
    public required OrderBookType OrderBookType { get; init; }

    /// <summary>
    /// Заявки на покупку.
    /// </summary>
    public required IReadOnlyList<OrderModel> Bids { get; init; }

    /// <summary>
    /// Заявки на продажу.
    /// </summary>
    public required IReadOnlyList<OrderModel> Asks { get; init; }
}