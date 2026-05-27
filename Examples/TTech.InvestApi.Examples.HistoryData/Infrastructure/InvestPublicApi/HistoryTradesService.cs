using TTech.InvestApi.Examples.HistoryData.Exceptions;

namespace TTech.InvestApi.Examples.HistoryData.Infrastructure.InvestPublicApi;

public interface IHistoryTradesService
{
    Task<Stream> GetHistoryTrades(DateOnly date, string instrumentId);
}

public class HistoryTradesService(HttpClient httpClient) : IHistoryTradesService
{
    public async Task<Stream> GetHistoryTrades(DateOnly date, string instrumentId)
    {
        try
        {
            var url = "/history-trades/" + date.ToString("yyyy-MM-dd") + "?instrumentId=" + instrumentId;
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new HistoryTradesArchiveNotFoundException("Requested history trades not found", ex);
        }
    }
}
