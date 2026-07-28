using System.Text.Json;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.Stream;
using Tinkoff.InvestApi.V1;

// Получаем токен из переменной окружения
var token = Environment.GetEnvironmentVariable("TOKEN");
// Или раскомментируйте строку ниже и замените <<TOKEN>> вашим токеном
// var token = "<<TOKEN>>";

// Собираем ServiceCollection с клиентом
var serviceCollection = new ServiceCollection();
serviceCollection.AddInvestApiClient((_, settings) =>
{
	settings.AccessToken = token;
});
var serviceProvider = serviceCollection.BuildServiceProvider();

var client = serviceProvider.GetRequiredService<InvestApiClient>();
var configuration = new ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(
	client.MarketDataStream, 
	(client, options) => client.MarketDataStream(options))
	.WithPingDelayMs(1000)
	.WithInactivityTimeoutMs(10000);

var subscribeCandlesRequest = new MarketDataRequest
{
	SubscribeCandlesRequest = new SubscribeCandlesRequest
	{
		Instruments =
		{
			new CandleInstrument
			{
				Figi = "BBG004730N88",
				Interval = SubscriptionInterval.OneMinute
			}
		},
		SubscriptionAction = SubscriptionAction.Subscribe
	}
};

var pingSettingsRequest = new MarketDataRequest
{
	PingSettings = new PingDelaySettings
	{
		PingDelayMs = 5000
	}
};
int i = 0;
var stream = new ResilenceDuplexStreamWrapper<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(
	configuration,
	async response =>
{
	Console.WriteLine(JsonSerializer.Serialize(response));
	Interlocked.Increment(ref i);
});

await stream.ConnectAsync();
await stream.CallAsync(subscribeCandlesRequest);
await stream.CallAsync(pingSettingsRequest);
await Task.Run(() =>
{
	while (true)
	{
		if (i == 10)
		{
			stream.Disconnect();
			break;
		}
	}
});
