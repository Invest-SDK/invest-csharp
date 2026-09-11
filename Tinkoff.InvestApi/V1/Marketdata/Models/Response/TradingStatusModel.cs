namespace Tinkoff.InvestApi.V1.Marketdata.Models.Response;

/// <summary>
/// Независимая модель торгового статуса, значения которой инициализируются копированием из grpc-модели.
/// </summary>
public record TradingStatusModel
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
    /// Статус торговли инструментом.
    /// </summary>
    public required SecurityTradingStatus TradingStatus { get; init; }

    /// <summary>
    /// Признак доступности выставления лимитной заявки по инструменту.
    /// </summary>
    public required bool LimitOrderAvailableFlag { get; init; }

    /// <summary>
    /// Признак доступности выставления рыночной заявки по инструменту.
    /// </summary>
    public required bool MarketOrderAvailableFlag { get; init; }

    /// <summary>
    /// Время изменения торгового статуса по UTC.
    /// </summary>
    public required DateTime? Time { get; init; }

    /// <summary>
    /// UID инструмента.
    /// </summary>
    public required string InstrumentUid { get; init; }
}