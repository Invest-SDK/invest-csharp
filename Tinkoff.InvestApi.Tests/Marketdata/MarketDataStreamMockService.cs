using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Tests.Marketdata;

/// <summary>
/// Мок двунаправленного стрима <c>MarketDataStreamService.MarketDataStream</c>.
/// Подтверждает подписки (Subscribe*Response) и по конфигурации отправляет push-события
/// (сделки, свечи, стаканы, торговые статусы) после подтверждения.
/// Конфигурация статическая и задаётся в тесте перед вызовом подписки; тесты одной коллекции выполняются
/// последовательно, поэтому статическое состояние между ними безопасно.
/// </summary>
public class MarketDataStreamMockService : MarketDataStreamService.MarketDataStreamServiceBase
{
    private readonly ILogger<MarketDataStreamMockService> _logger;

    /// <summary>
    /// Код тикера и класса, который эхо-возвращается в подтверждениях и проставляется в push-событиях.
    /// Формат: "TICKER_CLASS". Клиент формирует такой же TickerClassCode у MarketDataInstrument.
    /// </summary>
    public static string TickerClassCode { get; set; } = "TICKER_CLASS";

    /// <summary>
    /// Задержка перед отправкой push-события после подтверждения подписки.
    /// Даёт клиенту время зарегистрировать локальный обработчик.
    /// </summary>
    public static int PushDelayMs { get; set; } = 300;

    /// <summary>
    /// Отправлять ли сделку после подтверждения подписки на сделки.
    /// </summary>
    public static bool PushTradesAfterSubscribe { get; set; } = true;

    /// <summary>
    /// Отправлять ли свечу после подтверждения подписки на свечи.
    /// </summary>
    public static bool PushCandlesAfterSubscribe { get; set; } = true;

    /// <summary>
    /// Отправлять ли стакан после подтверждения подписки на стаканы.
    /// </summary>
    public static bool PushOrderBooksAfterSubscribe { get; set; } = true;

    /// <summary>
    /// Отправлять ли торговый статус после подтверждения подписки на статусы.
    /// </summary>
    public static bool PushTradingStatusAfterSubscribe { get; set; } = true;

    /// <summary>
    /// Тип стакана, который отправляется в push-событии (не зависит от типа подписки).
    /// </summary>
    public static OrderBookType OrderBookPushType { get; set; } = OrderBookType.Exchange;

    /// <summary>
    /// Дополнительная пауза перед единственным push торгового статуса (чтобы оба обработчика успели зарегистрироваться).
    /// </summary>
    public static int TradingStatusPushDelayMs { get; set; } = 0;

    private static int _tradingStatusPushed;

    /// <summary>
    /// Записи входящих запросов на подписку/отписку свечей по каждому стриму.
    /// StreamSeq — номер server-инвокации MarketDataStream (порядковый, по одному на стрим клиента);
    /// используется тестами для проверки маршрутизации между стримами пула.
    /// </summary>
    public static readonly ConcurrentBag<(int StreamSeq, SubscriptionAction Action, string Figi, SubscriptionInterval Interval)> CandleRequests = new();

    /// <summary>
    /// Порядковый номер стрима, инкрементируется на каждую server-инвокацию MarketDataStream.
    /// </summary>
    private static int _streamSeq;

    public MarketDataStreamMockService(ILogger<MarketDataStreamMockService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Сбрасывает статическую конфигурацию в значения по умолчанию.
    /// </summary>
    public static void Reset()
    {
        TickerClassCode = "TICKER_CLASS";
        PushDelayMs = 300;
        PushTradesAfterSubscribe = true;
        PushCandlesAfterSubscribe = true;
        PushOrderBooksAfterSubscribe = true;
        PushTradingStatusAfterSubscribe = true;
        OrderBookPushType = OrderBookType.Exchange;
        TradingStatusPushDelayMs = 0;
        Interlocked.Exchange(ref _tradingStatusPushed, 0);
        Interlocked.Exchange(ref _streamSeq, 0);
        CandleRequests.Clear();
    }

    public override async Task MarketDataStream(
        IAsyncStreamReader<MarketDataRequest> requestStream,
        IServerStreamWriter<MarketDataResponse> responseStream,
        ServerCallContext context)
    {
        var streamSeq = Interlocked.Increment(ref _streamSeq);
        try
        {
            while (await requestStream.MoveNext(context.CancellationToken))
            {
                var request = requestStream.Current;
                _logger.LogDebug("Mock received request: {Request}", Describe(request));

                if (request.SubscribeCandlesRequest != null)
                {
                    foreach (var i in request.SubscribeCandlesRequest.Instruments)
                    {
                        CandleRequests.Add((streamSeq, request.SubscribeCandlesRequest.SubscriptionAction, i.Figi, i.Interval));
                    }
                }

                if (request.SubscribeTradesRequest != null)
                {
                    await ConfirmTradesAndPushAsync(request.SubscribeTradesRequest, responseStream, context);
                }
                else if (request.SubscribeCandlesRequest != null)
                {
                    await ConfirmCandlesAndPushAsync(request.SubscribeCandlesRequest, responseStream, context);
                }
                else if (request.SubscribeOrderBookRequest != null)
                {
                    await ConfirmOrderBooksAndPushAsync(request.SubscribeOrderBookRequest, responseStream, context);
                }
                else if (request.SubscribeInfoRequest != null)
                {
                    await ConfirmInfoAndPushAsync(request.SubscribeInfoRequest, responseStream, context);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение стрима (Dispose).
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            // Нормальное завершение стрима (Dispose).
        }
    }

    private static async Task ConfirmTradesAndPushAsync(
        SubscribeTradesRequest sub,
        IServerStreamWriter<MarketDataResponse> responseStream,
        ServerCallContext context)
    {
        SplitTickerClassCode(out var ticker, out var classCode);

        var confirmation = new MarketDataResponse
        {
            SubscribeTradesResponse = new SubscribeTradesResponse
            {
                TradeSubscriptions =
                {
                    sub.Instruments.Select(i => new TradeSubscription
                    {
                        Figi = i.Figi,
                        InstrumentUid = string.IsNullOrEmpty(i.InstrumentId) ? i.Figi : i.InstrumentId,
                        SubscriptionStatus = SubscriptionStatus.Success,
                        SubscriptionAction = SubscriptionAction.Subscribe,
                        Ticker = ticker,
                        ClassCode = classCode
                    })
                }
            }
        };
        await responseStream.WriteAsync(confirmation);

        if (PushTradesAfterSubscribe)
        {
            await Task.Delay(PushDelayMs, context.CancellationToken);
            foreach (var i in sub.Instruments)
            {
                await responseStream.WriteAsync(new MarketDataResponse
                {
                    Trade = new Trade
                    {
                        Figi = i.Figi,
                        InstrumentUid = string.IsNullOrEmpty(i.InstrumentId) ? i.Figi : i.InstrumentId,
                        Ticker = ticker,
                        ClassCode = classCode,
                        Direction = TradeDirection.Buy,
                        Price = new Quotation { Units = 1 },
                        Quantity = 1,
                        TradeSource = TradeSourceType.TradeSourceAll,
                        Time = Timestamp.FromDateTime(DateTime.UtcNow)
                    }
                });
            }
        }
    }

    private static async Task ConfirmCandlesAndPushAsync(
        SubscribeCandlesRequest sub,
        IServerStreamWriter<MarketDataResponse> responseStream,
        ServerCallContext context)
    {
        SplitTickerClassCode(out var ticker, out var classCode);

        var confirmation = new MarketDataResponse
        {
            SubscribeCandlesResponse = new SubscribeCandlesResponse
            {
                CandlesSubscriptions =
                {
                    sub.Instruments.Select(i => new CandleSubscription
                    {
                        Figi = i.Figi,
                        InstrumentUid = string.IsNullOrEmpty(i.InstrumentId) ? i.Figi : i.InstrumentId,
                        Interval = i.Interval,
                        SubscriptionStatus = SubscriptionStatus.Success,
                        SubscriptionAction = SubscriptionAction.Subscribe,
                        Ticker = ticker,
                        ClassCode = classCode
                    })
                }
            }
        };
        await responseStream.WriteAsync(confirmation);

        if (PushCandlesAfterSubscribe)
        {
            await Task.Delay(PushDelayMs, context.CancellationToken);
            foreach (var i in sub.Instruments)
            {
                var now = Timestamp.FromDateTime(DateTime.UtcNow);
                await responseStream.WriteAsync(new MarketDataResponse
                {
                    Candle = new Candle
                    {
                        Figi = i.Figi,
                        InstrumentUid = string.IsNullOrEmpty(i.InstrumentId) ? i.Figi : i.InstrumentId,
                        Interval = i.Interval,
                        Ticker = ticker,
                        ClassCode = classCode,
                        Open = new Quotation { Units = 1 },
                        High = new Quotation { Units = 2 },
                        Low = new Quotation { Units = 1 },
                        Close = new Quotation { Units = 2 },
                        Volume = 10,
                        Time = now,
                        LastTradeTs = now
                    }
                });
            }
        }
    }

    private static async Task ConfirmOrderBooksAndPushAsync(
        SubscribeOrderBookRequest sub,
        IServerStreamWriter<MarketDataResponse> responseStream,
        ServerCallContext context)
    {
        SplitTickerClassCode(out var ticker, out var classCode);

        var confirmation = new MarketDataResponse
        {
            SubscribeOrderBookResponse = new SubscribeOrderBookResponse
            {
                OrderBookSubscriptions =
                {
                    sub.Instruments.Select(i => new OrderBookSubscription
                    {
                        Figi = i.Figi,
                        InstrumentUid = string.IsNullOrEmpty(i.InstrumentId) ? i.Figi : i.InstrumentId,
                        Depth = i.Depth,
                        OrderBookType = i.OrderBookType,
                        SubscriptionStatus = SubscriptionStatus.Success,
                        SubscriptionAction = SubscriptionAction.Subscribe,
                        Ticker = ticker,
                        ClassCode = classCode
                    })
                }
            }
        };
        await responseStream.WriteAsync(confirmation);

        if (PushOrderBooksAfterSubscribe)
        {
            await Task.Delay(PushDelayMs, context.CancellationToken);
            foreach (var i in sub.Instruments)
            {
                await responseStream.WriteAsync(new MarketDataResponse
                {
                    Orderbook = new OrderBook
                    {
                        Figi = i.Figi,
                        InstrumentUid = string.IsNullOrEmpty(i.InstrumentId) ? i.Figi : i.InstrumentId,
                        Depth = i.Depth,
                        OrderBookType = OrderBookPushType,
                        Ticker = ticker,
                        ClassCode = classCode,
                        IsConsistent = true,
                        Time = Timestamp.FromDateTime(DateTime.UtcNow)
                    }
                });
            }
        }
    }

    private static async Task ConfirmInfoAndPushAsync(
        SubscribeInfoRequest sub,
        IServerStreamWriter<MarketDataResponse> responseStream,
        ServerCallContext context)
    {
        SplitTickerClassCode(out var ticker, out var classCode);

        var confirmation = new MarketDataResponse
        {
            SubscribeInfoResponse = new SubscribeInfoResponse
            {
                InfoSubscriptions =
                {
                    sub.Instruments.Select(i => new InfoSubscription
                    {
                        Figi = i.Figi,
                        InstrumentUid = string.IsNullOrEmpty(i.InstrumentId) ? i.Figi : i.InstrumentId,
                        SubscriptionStatus = SubscriptionStatus.Success,
                        SubscriptionAction = SubscriptionAction.Subscribe,
                        Ticker = ticker,
                        ClassCode = classCode
                    })
                }
            }
        };
        await responseStream.WriteAsync(confirmation);

        // Торговый статус шлём один раз за тест (единственный пуш) в фоновой задаче:
        // серверный цикл читает запросы последовательно, поэтому inline-ожидание задержки
        // заблокировало бы обработку следующих подписок, и их обработчики не успели бы зарегистрироваться
        // до пуша. Фоновая задача даёт циклу дочитать остальные подписки.
        if (PushTradingStatusAfterSubscribe && Interlocked.Exchange(ref _tradingStatusPushed, 1) == 0)
        {
            var delay = TradingStatusPushDelayMs > 0 ? TradingStatusPushDelayMs : PushDelayMs;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, context.CancellationToken);
                    foreach (var i in sub.Instruments)
                    {
                        await responseStream.WriteAsync(new MarketDataResponse
                        {
                            TradingStatus = new TradingStatus
                            {
                                Figi = i.Figi,
                                InstrumentUid = string.IsNullOrEmpty(i.InstrumentId) ? i.Figi : i.InstrumentId,
                                Ticker = ticker,
                                ClassCode = classCode,
                                TradingStatus_ = SecurityTradingStatus.NormalTrading,
                                Time = Timestamp.FromDateTime(DateTime.UtcNow),
                                LimitOrderAvailableFlag = true,
                                MarketOrderAvailableFlag = true
                            }
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    // Нормальное завершение стрима (Dispose).
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    // Нормальное завершение стрима (Dispose).
                }
            });
        }
    }

    private static void SplitTickerClassCode(out string ticker, out string classCode)
    {
        var parts = TickerClassCode.Split('_', 2);
        ticker = parts[0];
        classCode = parts.Length > 1 ? parts[1] : string.Empty;
    }

    private static string Describe(MarketDataRequest request)
    {
        if (request.SubscribeTradesRequest != null)
            return "SubscribeTrades";
        if (request.SubscribeCandlesRequest != null)
            return "SubscribeCandles";
        if (request.SubscribeOrderBookRequest != null)
            return "SubscribeOrderBook";
        if (request.SubscribeInfoRequest != null)
            return "SubscribeInfo";
        return request.PayloadCase.ToString();
    }
}