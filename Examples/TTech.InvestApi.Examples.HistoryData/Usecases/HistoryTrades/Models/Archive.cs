namespace TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Models;

public record Archive(
    Guid Id,
    string InstrumentId,
    DateOnly Date,
    string BucketName,
    string FileName);
