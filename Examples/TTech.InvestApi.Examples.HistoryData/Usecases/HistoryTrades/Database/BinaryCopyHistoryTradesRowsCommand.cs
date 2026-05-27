using Tinkoff.InvestApi.V1.History;
using TTech.InvestApi.Examples.HistoryData.Infrastructure.Database;

namespace TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Database;

public interface IBinaryCopyHistoryTradesRowsCommand
{
    Task Import(IReadOnlyCollection<IHistoryTrade> rows, Guid archiveId, CancellationToken cancellationToken);
}

public class BinaryCopyHistoryTradesRowsCommand(IUnitOfWork unitOfWork) : IBinaryCopyHistoryTradesRowsCommand
{
    private const string Sql =
        "COPY t_history_trade (trade_ts, ticker_cc, direction, price, quantity, trade_source, instrument_uid, archive_id) FROM STDIN (FORMAT BINARY)";

    public async Task Import(IReadOnlyCollection<IHistoryTrade> csvRecords, Guid archiveId, CancellationToken cancellationToken)
    {
        var connection = unitOfWork.GetConnection();
        await using var importer = await connection.BeginBinaryImportAsync(Sql, cancellationToken);
        foreach (var csvRecord in csvRecords)
        {
            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(csvRecord.TradeTs, NpgsqlTypes.NpgsqlDbType.TimestampTz, cancellationToken);
            await importer.WriteAsync(csvRecord.TickerCc, NpgsqlTypes.NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(csvRecord.Direction, NpgsqlTypes.NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(csvRecord.Price, NpgsqlTypes.NpgsqlDbType.Numeric, cancellationToken);
            await importer.WriteAsync(csvRecord.Quantity, NpgsqlTypes.NpgsqlDbType.Integer, cancellationToken);
            await importer.WriteAsync(csvRecord.TradeSource, NpgsqlTypes.NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(csvRecord.InstrumentUid, NpgsqlTypes.NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(archiveId, NpgsqlTypes.NpgsqlDbType.Uuid, cancellationToken);
        }
        await importer.CompleteAsync(cancellationToken);
    }
}
