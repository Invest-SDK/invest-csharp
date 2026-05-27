using Microsoft.Extensions.DependencyInjection;
using TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades;
using TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Database;
using TTech.InvestApi.Examples.HistoryData.Usecases.HistoryTrades.Services;

namespace TTech.InvestApi.Examples.HistoryData.Usecases;

public static class DependencyInjection
{
    public static void AddUsecases(this IServiceCollection services)
    {
        services.AddHistoryTradesUsecase();
        services.AddHistoryCandlesUsecase();
    }

    private static void AddHistoryTradesUsecase(this IServiceCollection services)
    {
        // database
        services.AddTransient<IInsertArchiveCommand, InsertArchiveCommand>();
        services.AddTransient<ISelectArchiveCommand, SelectArchiveCommand>();
        services.AddTransient<IBinaryCopyHistoryTradesRowsCommand, BinaryCopyHistoryTradesRowsCommand>();
        // service
        services.AddTransient<IDatabaseService, DatabaseService>();
        // usecase
        services.AddTransient<IHistoryTradesUsecase, HistoryTradesUsecase>();
    }

    private static void AddHistoryCandlesUsecase(this IServiceCollection services)
    {
        // usecase
        services.AddTransient<IHistoryCandlesUsecase, HistoryCandlesUsecase>();
    }
}
