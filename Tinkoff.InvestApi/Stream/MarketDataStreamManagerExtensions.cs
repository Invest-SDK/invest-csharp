using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.Stream;
using Tinkoff.InvestApi.Stream.Marketdata;

namespace Microsoft.Extensions.DependencyInjection;

public static class MarketDataStreamManagerExtensions
{
    /// <summary>
    /// Регистрирует <see cref="MarketDataStreamManager"/> как singleton-сервис.
    /// Соединение берётся из уже зарегистрированного <see cref="InvestApiClient"/> (его RetriableInvoker),
    /// переиспользуя auth/ssl/retry, настроенные <see cref="InvestApiClientExtensions.AddInvestApiClient"/>.
    /// Поэтому <c>AddInvestApiClient</c> должен быть вызван до этого метода.
    /// Контейнер сам вызывает DisposeAsync менеджера при закрытии.
    /// </summary>
    public static IServiceCollection AddMarketDataStreamManager(
        this IServiceCollection services,
        Action<MarketDataManagerConfigurer>? configureManager = null)
    {
        return services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<InvestApiClient>();
            var configurer = new MarketDataManagerConfigurer(client.RetriableInvoker);
            configureManager?.Invoke(configurer);
            return new MarketDataStreamManager(configurer, sp.GetService<ILoggerFactory>());
        });
    }
}