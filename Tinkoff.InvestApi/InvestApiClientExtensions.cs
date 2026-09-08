using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1.Cache;
using Tinkoff.InvestApi.V1.History;

namespace Microsoft.Extensions.DependencyInjection;

public static class InvestApiClientExtensions
{
    private const string DefaultName = "";
    private const string publicApiUri = "https://invest-public-api.tbank.ru:443";
    private const string sandboxApiUri = "https://sandbox-invest-public-api.tbank.ru:443";
    private const string certificatePath = "Tinkoff.InvestApi.Resources.Certificates.Russian_Trusted_Root_CA.cer";

    public static IServiceCollection AddInvestApiClient(this IServiceCollection services,
        Action<IServiceProvider, InvestApiSettings> configureSettings)
    {
        return AddInvestApiClient(services, DefaultName, configureSettings);
    }

    public static IServiceCollection AddInvestApiClient(this IServiceCollection services, string name,
        Action<IServiceProvider, InvestApiSettings> configureSettings)
    {
        services.AddConfiguration();
        services.AddGrpcClient<InvestApiClient>(name,
                (serviceProvider, options) =>
                {
                    var settings = new InvestApiSettings();
                    configureSettings(serviceProvider, settings);
                    options.Address = settings.Sandbox
                        ? new Uri(sandboxApiUri)
                        : new Uri(publicApiUri);
                })
                .ConfigureChannel((serviceProvider, options) =>
                {
                    options.MaxReceiveMessageSize = null;
                    var settings = new InvestApiSettings();
                    configureSettings(serviceProvider, settings);
                    var accessToken = settings.AccessToken ??
                                    throw new InvalidOperationException("AccessToken is required");
                    var appName = string.IsNullOrEmpty(settings.AppName)
                        ? "tinkoff.invest-api-csharp-sdk"
                        : settings.AppName;
                    var credentials = CallCredentials.FromInterceptor((_, metadata) =>
                    {
                        metadata.Add("Authorization", $"Bearer {accessToken}");
                        if (appName != null)
                        {
                            metadata.Add("x-app-name", appName);
                        }

                        return Task.CompletedTask;
                    });
                    options.HttpHandler = BuildHttpMessageHandler(settings.EnableSsl);
                    options.Credentials = ChannelCredentials.Create(new SslCredentials(), credentials);

                    var defaultMethodConfig = new MethodConfig
                    {
                        Names = { MethodName.Default },
                        RetryPolicy = new RetryPolicy
                        {
                            MaxAttempts = 5,
                            InitialBackoff = TimeSpan.FromSeconds(1),
                            MaxBackoff = TimeSpan.FromSeconds(5),
                            BackoffMultiplier = 1.5,
                            RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.Internal }
                        }
                    };

                    options.ServiceConfig = new ServiceConfig { MethodConfigs = { defaultMethodConfig } };
                });
        services.AddHttpClient<IHistoryService, HistoryService>(x => x.BaseAddress = new Uri(publicApiUri));
        services.AddCache();
        return services;
    }

    private static HttpMessageHandler BuildHttpMessageHandler(bool enableSsl)
    {
        if (enableSsl)
        {
            return new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = CustomCertificateValidationCallback()
                }
            };
        } else {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
        }
    }

    private static System.Net.Security.RemoteCertificateValidationCallback CustomCertificateValidationCallback()
    {
        var certificate = GetCertificate();
        return (sender, cert, chain, sslPolicyErrors) =>
        {
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                return true;

            if (cert == null || cert is not X509Certificate2 serverCert)
                return false;

            using var validationChain = new X509Chain();

            validationChain.ChainPolicy.ExtraStore.Add(certificate);
            validationChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            validationChain.ChainPolicy.CustomTrustStore.Add(certificate);
            validationChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            return validationChain.Build(serverCert);
        };
    }

    private static IServiceCollection AddCache(this IServiceCollection services)
    {
        services.AddOptions<CacheOptions>().BindConfiguration(CacheOptions.CacheSection);
        services.AddMemoryCache();
        services.AddSingleton<IInstrumentCache, InstrumentCache>();
        services.AddTransient<IInstrumentCacheService, InstrumentCacheService>();
        return services;
    }

    private static IServiceCollection AddConfiguration(this IServiceCollection services)
    {
        if (services.Any(x => x.ServiceType == typeof(IConfiguration)))
        {
            return services;
        }
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        services.AddSingleton(configuration);
        return services;
    }

    private static X509Certificate2 GetCertificate()
    {
        var assambly = typeof(InvestApiClientExtensions).Assembly;
        using var stream = assambly.GetManifestResourceStream(certificatePath);
        if (stream != null)
        {
            using var memStream = new MemoryStream();
            stream.CopyTo(memStream);
            return new X509Certificate2(memStream.ToArray());
        }
        throw new FileNotFoundException();
    }
}
