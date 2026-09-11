namespace Tinkoff.InvestApi.V1.Marketdata.Models.Wrapper.Response;

/// <summary>
/// Независимая модель открытого интереса, значения которой инициализируются копированием из grpc-модели.
/// </summary>
public record OpenInterestWrapper
{
    /// <summary>
    /// Тикер инструмента.
    /// </summary>
    public required string Ticker { get; init; }

    /// <summary>
    /// Класс-код инструмента.
    /// </summary>
    public required string ClassCode { get; init; }

    /// <summary>
    /// Открытый интерес.
    /// </summary>
    public required long OpenInterest { get; init; }

    /// <summary>
    /// UID инструмента.
    /// </summary>
    public required string InstrumentUid { get; init; }
}