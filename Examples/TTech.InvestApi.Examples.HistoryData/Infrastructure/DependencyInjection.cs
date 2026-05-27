using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using TTech.InvestApi.Examples.HistoryData.Infrastructure.Database;
using TTech.InvestApi.Examples.HistoryData.Infrastructure.InvestPublicApi;
using TTech.InvestApi.Examples.HistoryData.Infrastructure.S3;

namespace TTech.InvestApi.Examples.HistoryData.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services)
    {
        services.AddDatabase();
        services.AddInvestPublicApi();
        services.AddS3();
    }

    private static void AddDatabase(this IServiceCollection services)
    {
        services.AddSingleton<IDataSourceService, DataSourceService>();
        services.AddScoped<UnitOfWork>();
        services.AddTransient<IUnitOfWork>(p => p.GetRequiredService<UnitOfWork>());
    }

    private static void AddInvestPublicApi(this IServiceCollection services)
    {
        services.AddHttpClient<IHistoryTradesService, HistoryTradesService>(x =>
            x.BaseAddress = new Uri("https://invest-public-api.tinkoff.ru"));
    }

    private static void AddS3(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonS3>(p =>
            new AmazonS3Client("minioaccesskey", "miniosecretkey", new AmazonS3Config()
            {
                ServiceURL = "http://localhost:9000",
                ForcePathStyle = true,
                UseHttp = true,
                Timeout = TimeSpan.FromSeconds(10),
                MaxErrorRetry = 3
            }));
        services.AddTransient<IS3Service, S3Service>();
    }
}
