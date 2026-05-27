using Npgsql;

namespace TTech.InvestApi.Examples.HistoryData.Infrastructure.Database;

public interface IDataSourceService
{
    NpgsqlDataSource GetDataSource();
}

public class DataSourceService : IDataSourceService
{
    private const string connectionString = "Host=localhost;Username=postgres;Password=postgres;Database=history_data";
    private readonly NpgsqlDataSource _dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
    
    public NpgsqlDataSource GetDataSource()
    {
        return _dataSource;
    }
}
