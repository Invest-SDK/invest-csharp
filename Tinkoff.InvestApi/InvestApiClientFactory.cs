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
        var client = new ServiceCollection()
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