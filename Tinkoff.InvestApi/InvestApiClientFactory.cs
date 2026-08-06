using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tinkoff.InvestApi;

public static class InvestApiClientFactory
{
    public static InvestApiClient Create(string accessToken, bool sandbox = false)
    {
        var settings = new InvestApiSettings()
        {
            AccessToken = accessToken,
            Sandbox = sandbox
        };
        return Create(settings);
    }  
    
    public static InvestApiClient Create(InvestApiSettings settings)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        var client = new ServiceCollection()
            .AddSingleton(configuration)
            .AddInvestApiClient((_, clientSettings) =>
            {
                clientSettings.AccessToken = settings.AccessToken;
                clientSettings.Sandbox = settings.Sandbox;
                clientSettings.AppName = settings.AppName;
                clientSettings.Timeout = settings.Timeout;
            })
            .BuildServiceProvider()
            .GetRequiredService<InvestApiClient>();
        
        client.RetriableInvoker.DefaultTimeout = settings.Timeout ?? 30000;

        return client;
    }
}