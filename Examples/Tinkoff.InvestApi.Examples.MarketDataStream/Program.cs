﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.Examples.MarketDataStream;

// Получаем токен из переменной окружения
var token = Environment.GetEnvironmentVariable("TOKEN");
// Или раскомментируйте строку ниже и замените <<TOKEN>> вашим токеном
// var token = "<<TOKEN>>";

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddInvestApiClient((_, settings) =>
{
    settings.AccessToken = token;
});

builder.Services.AddTransient<Example1>();

using IHost host = builder.Build();
var example = host.Services.GetRequiredService<Example1>();
await example.Run();
