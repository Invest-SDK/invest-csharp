using Npgsql;

namespace TTech.InvestApi.Examples.HistoryData.Infrastructure.Database;

public interface IUnitOfWork
{
    NpgsqlConnection GetConnection();
    Task BeginTransaction(CancellationToken cancellationToken);
    Task CommitTransaction(CancellationToken cancellationToken);
    Task RollbackTransaction(CancellationToken cancellationToken);
}

public class UnitOfWork(IDataSourceService dataSourceService) : IUnitOfWork, IAsyncDisposable
{
    private NpgsqlTransaction? _transaction;

    public NpgsqlConnection GetConnection()
    {
        return _transaction?.Connection
            ?? throw new InvalidOperationException("Transaction is not started");
    }

    public async Task BeginTransaction(CancellationToken cancellationToken)
    {
        var connection = await dataSourceService.GetDataSource().OpenConnectionAsync(cancellationToken);
        _transaction = await connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransaction(CancellationToken cancellationToken)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task RollbackTransaction(CancellationToken cancellationToken)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
