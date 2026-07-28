using System.Globalization;
using System.IO.Compression;

namespace Tinkoff.InvestApi.V1.History;

public interface IHistoryService
{
    IAsyncEnumerable<HistoryCandle> GetHistoryData(int year, Guid instrumentId);
    IAsyncEnumerable<HistoryTrade> GetHistoryTrades(DateTime date);
    IAsyncEnumerable<HistoryTrade> GetHistoryTrades(DateTime date, string instrumentId);
    Task<Stream> GetHistoryTradesCsv(DateTime date);
    Task<Stream> GetHistoryTradesCsv(DateTime date, string instrumentId);
}

public class HistoryService(HttpClient httpClient) : IHistoryService
{
    private const char csvDelimiterTrade = ',';
    private const char csvDelimiterCandle = ';';
    private static readonly CultureInfo csvCultureInfo = CultureInfo.GetCultureInfo("en-US");

    public async IAsyncEnumerable<HistoryCandle> GetHistoryData(int year, Guid instrumentId)
    {
        var url = "/history-data?instrumentId=" + instrumentId.ToString() + "&year=" + year.ToString();
        using var downloadedStream = await httpClient.GetStreamAsync(url);
        using var archive = new ZipArchive(downloadedStream, ZipArchiveMode.Read);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            string? csvLine;
            while ((csvLine = await reader.ReadLineAsync()) != null)
            {
                yield return ParseHistoryCandle(csvLine);
            }
        }
    }

    public async IAsyncEnumerable<HistoryTrade> GetHistoryTrades(DateTime date)
    {
        var url = "/history-trades/" + date.ToString("yyyy-MM-dd");
        using var downloadedStream = await httpClient.GetStreamAsync(url);
        using var gzipStream = new GZipStream(downloadedStream, CompressionMode.Decompress);
        using var gzipStreamReader = new StreamReader(gzipStream);
        var csvLine = await gzipStreamReader.ReadLineAsync(); // skip header
        while ((csvLine = await gzipStreamReader.ReadLineAsync()) != null)
        {
            yield return ParseHistoryTrade(csvLine);
        }
    }

    public async IAsyncEnumerable<HistoryTrade> GetHistoryTrades(DateTime date, string instrumentId)
    {
        var url = "/history-trades/" + date.ToString("yyyy-MM-dd") + "?instrumentId=" + instrumentId;
        using var downloadedStream = await httpClient.GetStreamAsync(url);
        using var gzipStream = new GZipStream(downloadedStream, CompressionMode.Decompress);
        using var gzipStreamReader = new StreamReader(gzipStream);
        var csvLine = await gzipStreamReader.ReadLineAsync(); // skip header
        while ((csvLine = await gzipStreamReader.ReadLineAsync()) != null)
        {
            yield return ParseHistoryTrade(csvLine);
        }
    }

    public async Task<Stream> GetHistoryTradesCsv(DateTime date)
    {
        var url = "/history-trades/" + date.ToString("yyyy-MM-dd");
        return await GetCsv(url);
    }

    public async Task<Stream> GetHistoryTradesCsv(DateTime date, string instrumentId)
    {
        var url = "/history-trades/" + date.ToString("yyyy-MM-dd") + "?instrumentId=" + instrumentId;
        return await GetCsv(url);
    }

    private async Task<Stream> GetCsv(string url)
    {
        var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStreamAsync();
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new HistoryArchiveNotFoundException("History CSV archive not found");
        throw new HttpRequestException("Failed to download history CSV archive");
    }

    private HistoryTrade ParseHistoryTrade(string csvLine)
    {
        var csvSpan = csvLine.AsSpan();
        Span<Range> ranges = stackalloc Range[7];
        csvSpan.Split(ranges, csvDelimiterTrade);
        return new HistoryTrade
        {
            TradeTs = DateTimeOffset.Parse(csvSpan[ranges[0]]),
            TickerCc = csvSpan[ranges[1]].ToString(),
            Direction = csvSpan[ranges[2]].ToString(),
            Price = decimal.Parse(csvSpan[ranges[3]], csvCultureInfo),
            Quantity = long.Parse(csvSpan[ranges[4]]),
            TradeSource = csvSpan[ranges[5]].ToString(),
            InstrumentUid = Guid.TryParse(csvSpan[ranges[6]], out var uid)
                ? uid
                : null
        };
    }

    private HistoryCandle ParseHistoryCandle(string csvLine)
    {
        var csvSpan = csvLine.AsSpan();
        Span<Range> ranges = stackalloc Range[8]; // one extra delimiter at the end
        csvSpan.Split(ranges, csvDelimiterCandle);
        return new HistoryCandle
        {
            InstrumentUid = Guid.Parse(csvSpan[ranges[0]]),
            Utc = DateTimeOffset.Parse(csvSpan[ranges[1]]),
            Open = decimal.Parse(csvSpan[ranges[2]], csvCultureInfo),
            Close = decimal.Parse(csvSpan[ranges[3]], csvCultureInfo),
            High = decimal.Parse(csvSpan[ranges[4]], csvCultureInfo),
            Low = decimal.Parse(csvSpan[ranges[5]], csvCultureInfo),
            Volume = long.Parse(csvSpan[ranges[6]])
        };
    }
}
