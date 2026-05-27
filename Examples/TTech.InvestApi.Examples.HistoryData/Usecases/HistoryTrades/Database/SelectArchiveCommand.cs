using Npgsql;
using TTech.InvestApi.Examples.HistoryData.Infrastructure.Database;
using TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Models;

namespace TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Database;

public interface ISelectArchiveCommand
{
    Task<Archive?> Select(DateOnly date, string instrumentId, CancellationToken cancellationToken);
}

public class SelectArchiveCommand(IDataSourceService dataSourceService) : ISelectArchiveCommand
{
    private const string Sql =
        "select id, bucket_name, file_name from t_archive where date = @date and instrument_id = @instrument_id";

    public async Task<Archive?> Select(DateOnly date, string instrumentId, CancellationToken cancellationToken)
    {
        using var connection = await dataSourceService.GetDataSource().OpenConnectionAsync(cancellationToken);
        using var cmd = new NpgsqlCommand(Sql, connection);
        cmd.Parameters.AddWithValue("date", date);
        cmd.Parameters.AddWithValue("instrument_id", instrumentId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            var bucketName = reader.GetString(1);
            var fileName = reader.GetString(2);
            return new Archive(id, instrumentId, date, bucketName, fileName);
        }
        return null;
    }
}
