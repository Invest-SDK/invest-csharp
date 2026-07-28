namespace TTech.InvestApi.Examples.InstrumentsCache;

public record TestInstrument(
    string Uid,
    string Figi,
    string Ticker,
    string ClassCode,
    TestInstrumentType Type);
