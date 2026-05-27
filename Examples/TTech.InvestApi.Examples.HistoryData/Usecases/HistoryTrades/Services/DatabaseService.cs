using System.Globalization;
using System.IO.Compression;
using System.Text;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.V1.History;
using TTech.InvestApi.Examples.HistoryData.Infrastructure.Database;
using TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Database;
using TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Models;

namespace TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Services;

public interface IDatabaseService
{
    Task<bool> ArchiveExists(DateOnly date, string instrumentId, CancellationToken cancellationToken);
    Task Persist(Stream stream, Archive archive, CancellationToken cancellationToken);
    Task Persist(IAsyncEnumerable<IHistoryTrade> trades, Archive archive, CancellationToken cancellationToken);
}

public class DatabaseService(
    ILogger<DatabaseService> logger,
    IUnitOfWork unitOfWork,
    IInsertArchiveCommand insertArchiveCommand,
    ISelectArchiveCommand selectArchiveCommand,
    IBinaryCopyHistoryTradesRowsCommand binaryCopyCommand) : IDatabaseService
{
    private const int batchSize = 1000;

    public async Task<bool> ArchiveExists(DateOnly date, string instrumentId, CancellationToken cancellationToken)
    {
        var existingArchive = await selectArchiveCommand.Select(date, instrumentId, cancellationToken);
        return existingArchive != null;
    }

    public async Task Persist(Stream stream, Archive archive, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }
        await using var decompressionStream = new GZipStream(stream, CompressionMode.Decompress);
        using var decompressionStreamReader = new StreamReader(decompressionStream, Encoding.UTF8);
        using var csvReader = new CsvReader(decompressionStreamReader, CultureInfo.InvariantCulture);
        var trades = csvReader.GetRecordsAsync<HistoryTradeCsvRecord>(cancellationToken);
        await Persist(trades, archive, cancellationToken);
    }

    public async Task Persist(IAsyncEnumerable<IHistoryTrade> trades, Archive archive, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.BeginTransaction(cancellationToken);
            await insertArchiveCommand.Insert(archive, cancellationToken);
            var batchOfCsvRecords = new List<IHistoryTrade>();
            await foreach (var trade in trades)
            {
                batchOfCsvRecords.Add(trade);
                if (batchOfCsvRecords.Count == batchSize)
                {
                    await binaryCopyCommand.Import(batchOfCsvRecords, archive.Id, cancellationToken);
                    batchOfCsvRecords.Clear();
                }
            }
            if (batchOfCsvRecords.Count > 0)
            {
                await binaryCopyCommand.Import(batchOfCsvRecords, archive.Id, cancellationToken);
            }
            await unitOfWork.CommitTransaction(cancellationToken);
            logger.LogInformation("Archive for {Date} and {InstrumentId} persisted successfully", archive.Date, archive.InstrumentId);
        }
        catch
        {
            await unitOfWork.RollbackTransaction(cancellationToken);
            logger.LogError("Archive for {Date} and {InstrumentId} persist failed", archive.Date, archive.InstrumentId);
            throw;
        }
    }
}
