using Microsoft.Extensions.Logging;

namespace TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades;

public interface IHistoryCandlesUsecase
{
    Task GetHistoryCandles(int year, Guid instrumentId, CancellationToken cancellationToken);
}

public class HistoryCandlesUsecase(
    ILogger<HistoryCandlesUsecase> logger,
    Tinkoff.InvestApi.InvestApiClient investApiClient) : IHistoryCandlesUsecase
{
    public async Task GetHistoryCandles(int year, Guid instrumentId, CancellationToken cancellationToken)
    {
        var minuteCandles = investApiClient.History.GetHistoryData(year, instrumentId);
        await foreach (var candle in minuteCandles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("{Utc};{Open};{Close};{High};{Low};{Volume}",
                candle.Utc, candle.Open, candle.Close, candle.High, candle.Low, candle.Volume);
        }
    }
}
