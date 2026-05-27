using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TTech.InvestApi.Examples.HistoryData.Infrastructure.S3;
using TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Models;
using TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Services;

namespace TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades;

public interface IHistoryTradesUsecase
{
    Task GetHistoryTrades(DateOnly date, string instrumentId, CancellationToken cancellationToken);
}

public class HistoryTradesUsecase(
    ILogger<HistoryTradesUsecase> logger,
    Tinkoff.InvestApi.InvestApiClient investApiClient,
    IDatabaseService databaseService,
    IS3Service s3Service) : IHistoryTradesUsecase
{
    private const string bucketName = "history-data";
    private const string gzipMimeType = "application/gzip";

    private readonly Channel<byte[]> databaseChannel = Channel.CreateUnbounded<byte[]>();
    private readonly Channel<byte[]> s3Channel = Channel.CreateUnbounded<byte[]>();

    public async Task GetHistoryTrades(DateOnly date, string instrumentId, CancellationToken cancellationToken)
    {
        if (await databaseService.ArchiveExists(date, instrumentId, cancellationToken))
        {
            logger.LogWarning("Archive already exists for {Date} and {InstrumentId}", date, instrumentId);
            return;
        }

        var archive = new Archive(
            Id: Guid.CreateVersion7(),
            InstrumentId: instrumentId,
            Date: date,
            BucketName: bucketName,
            FileName: DefineArchiveFileName(date, instrumentId));

        var tasks = new List<Task>
        {
            PersistToDatabase(archive, cancellationToken),
            UploadToS3(archive, cancellationToken),
        };

        await Download(date, instrumentId, cancellationToken);

        await Task.WhenAll(tasks);
    }

    private async Task Download(DateOnly date, string instrumentId, CancellationToken cancellationToken)
    {
        const int bufferSize = 1024;
        using var originalStream = await investApiClient.History.GetHistoryTradesCsv(
            new DateTime(date, TimeOnly.MinValue), instrumentId);
        byte[] buffer = new byte[bufferSize];
        int bytesRead;
        while ((bytesRead = await originalStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            var data = new byte[bytesRead];
            Buffer.BlockCopy(buffer, 0, data, 0, bytesRead);
            await databaseChannel.Writer.WriteAsync(data, cancellationToken);
            await s3Channel.Writer.WriteAsync(data, cancellationToken);
        }
        databaseChannel.Writer.TryComplete();
        s3Channel.Writer.TryComplete();
    }

    private async Task PersistToDatabase(Archive archive, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await ReadChannel(databaseChannel.Reader, ms, cancellationToken);
        await databaseService.Persist(ms, archive, cancellationToken);
    }

    private async Task UploadToS3(Archive archive, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await ReadChannel(s3Channel.Reader, ms, cancellationToken);
        await s3Service.Upload(ms, archive.FileName, bucketName, gzipMimeType, cancellationToken);
    }

    private static async Task ReadChannel(ChannelReader<byte[]> reader, Stream destination, CancellationToken cancellationToken)
    {
        await foreach (var buffer in reader.ReadAllAsync(cancellationToken))
        {
            await destination.WriteAsync(buffer, cancellationToken);
        }
        await destination.FlushAsync(cancellationToken);
    }

    private static string DefineArchiveFileName(DateOnly date, string instrumentId)
    {
        return date.ToString("yyyyMMdd") + "-" + instrumentId + ".csv.gz";
    }
}
