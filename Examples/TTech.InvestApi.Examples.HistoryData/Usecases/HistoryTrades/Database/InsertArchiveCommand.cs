using Npgsql;
using TTech.InvestApi.Examples.HistoryData.Exceptions;
using TTech.InvestApi.Examples.HistoryData.Infrastructure.Database;
using TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Models;

namespace TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Database;

public interface IInsertArchiveCommand
{
    Task<int> Insert(Archive archive, CancellationToken cancellationToken);
}

public class InsertArchiveCommand(IUnitOfWork unitOfWork) : IInsertArchiveCommand
{
    private const string Sql = """
    insert into t_archive (id, instrument_id, date, bucket_name, file_name)
    values (@id, @instrument_id, @date, @bucket_name, @file_name)
    """;

    public async Task<int> Insert(Archive archive, CancellationToken cancellationToken)
    {
        try
        {
            var connection = unitOfWork.GetConnection();
            using var cmd = new NpgsqlCommand(Sql, connection);
            cmd.Parameters.AddWithValue("id", archive.Id);
            cmd.Parameters.AddWithValue("date", archive.Date);
            cmd.Parameters.AddWithValue("instrument_id", archive.InstrumentId);
            cmd.Parameters.AddWithValue("bucket_name", archive.BucketName);
            cmd.Parameters.AddWithValue("file_name", archive.FileName);
            return await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (NpgsqlException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateArchiveException("Archive already exists", ex);
        }
    }
}
