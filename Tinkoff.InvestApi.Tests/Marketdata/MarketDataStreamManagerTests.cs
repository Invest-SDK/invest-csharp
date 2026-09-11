using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Tinkoff.InvestApi.Stream;
using Tinkoff.InvestApi.Stream.Marketdata;
using Tinkoff.InvestApi.V1.Marketdata.Models;
using Tinkoff.InvestApi.V1;
using Xunit.Abstractions;

namespace Tinkoff.InvestApi.Tests.Marketdata;

/// <summary>
/// Интеграционные тесты MarketDataStreamManager поверх мока MarketDataStreamMockService.
/// Общая предпосылка: мокируется двунаправленный gRPC-стрим MarketDataStreamService/MarketDataStream,
/// создаётся MarketDataStreamManager; в конце каждого теста вызывается DisposeAsync.
/// </summary>
[Collection("MarketDataIntegration")]
public class MarketDataStreamManagerTests
{
    private readonly MarketDataGrpcServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public MarketDataStreamManagerTests(MarketDataGrpcServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task TestSubscribeTradesDoesNotDuplicateAcrossSeparateCalls()
    {
        _output.WriteLine($"[{nameof(TestSubscribeTradesDoesNotDuplicateAcrossSeparateCalls)}] Начало теста");
        MarketDataStreamMockService.Reset();

        var (manager, _) = await CreateStartedManagerAsync();
        try
        {
            var instrumentA = NewInstrument(figi: "FIGI_A", uid: "UID_A");
            var instrumentB = NewInstrument(figi: "FIGI_B", uid: "UID_B");
            var tradesForListenerA = new ConcurrentBag<string>();
            var tradesForListenerB = new ConcurrentBag<string>();

            _output.WriteLine($"[{nameof(TestSubscribeTradesDoesNotDuplicateAcrossSeparateCalls)}] Подписка на сделки: A={instrumentA.Uid}, B={instrumentB.Uid}");
            await manager.SubscribeTradesAsync(
                new[] { instrumentA },
                onTrade: t => { tradesForListenerA.Add(t.InstrumentUid); _output.WriteLine($"[A] сделка: {t.InstrumentUid}"); return Task.CompletedTask; });
            await manager.SubscribeTradesAsync(
                new[] { instrumentB },
                onTrade: t => { tradesForListenerB.Add(t.InstrumentUid); _output.WriteLine($"[B] сделка: {t.InstrumentUid}"); return Task.CompletedTask; });

            await WaitUntilAsync(() => tradesForListenerA.Count == 1 && tradesForListenerB.Count == 1,
                log: () => _output.WriteLine($"[{nameof(TestSubscribeTradesDoesNotDuplicateAcrossSeparateCalls)}] Ожидание доставки: A={tradesForListenerA.Count}, B={tradesForListenerB.Count}"));

            _output.WriteLine($"[{nameof(TestSubscribeTradesDoesNotDuplicateAcrossSeparateCalls)}] Доставлено: A=[{string.Join(", ", tradesForListenerA)}], B=[{string.Join(", ", tradesForListenerB)}]");
            tradesForListenerA.Should().ContainSingle().Which.Should().Be("UID_A");
            tradesForListenerB.Should().ContainSingle().Which.Should().Be("UID_B");
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task TestSubscribeByPartialInstrumentCompletes()
    {
        _output.WriteLine($"[{nameof(TestSubscribeByPartialInstrumentCompletes)}] Начало теста");
        MarketDataStreamMockService.Reset();

        var (manager, _) = await CreateStartedManagerAsync();
        try
        {
            // Инструмент задан только по FIGI (uid и TickerClassCode не заполнены).
            var instrument = NewInstrument(figi: "FIGI_ONLY", uid: string.Empty, tickerClassCode: string.Empty);
            var received = new ConcurrentBag<string>();

            // Подписка должна завершиться, даже если инструмент задан не полностью:
            // ответ сопоставляется с ожиданием по указанным полям, а не по полному ключу.
            _output.WriteLine($"[{nameof(TestSubscribeByPartialInstrumentCompletes)}] Подписка по FIGI_ONLY (без uid/tickerClassCode)");
            var subscribeTask = manager.SubscribeTradesAsync(
                new[] { instrument },
                onTrade: t => { received.Add(t.InstrumentUid); return Task.CompletedTask; });

            await Task.WhenAny(subscribeTask, Task.Delay(2000));
            subscribeTask.IsCompleted.Should().BeTrue("подписка по неполному инструменту не должна зависать");
            await subscribeTask;

            _output.WriteLine($"[{nameof(TestSubscribeByPartialInstrumentCompletes)}] Подписка завершена, ожидаем сделку...");
            await WaitUntilAsync(() => received.Count >= 1,
                log: () => _output.WriteLine($"[{nameof(TestSubscribeByPartialInstrumentCompletes)}] Ожидание сделки: {received.Count}"));

            // Мок при пустом uid подставляет figi, поэтому пуш приходит с полным id (figi + uid),
            // а обработчик был зарегистрирован только по figi — доставка работает за счёт частичного
            // сопоставления id в DispatchLocalAsync.
            _output.WriteLine($"[{nameof(TestSubscribeByPartialInstrumentCompletes)}] Получено сделок: [{string.Join(", ", received)}]");
            received.Should().ContainSingle().Which.Should().Be("FIGI_ONLY");
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task TestSubscribeCandlesWithDifferentIntervalsForSameInstrument()
    {
        _output.WriteLine($"[{nameof(TestSubscribeCandlesWithDifferentIntervalsForSameInstrument)}] Начало теста");
        MarketDataStreamMockService.Reset();

        var (manager, _) = await CreateStartedManagerAsync();
        try
        {
            // Один и тот же инструмент подписан на свечи с двумя разными интервалами.
            var instrumentOneMinute = NewInstrument(figi: "FIGI_A", uid: "UID_A", interval: SubscriptionInterval.OneMinute);
            var instrumentFiveMinutes = NewInstrument(figi: "FIGI_A", uid: "UID_A", interval: SubscriptionInterval.FiveMinutes);
            var candlesReceived = new ConcurrentBag<SubscriptionInterval>();

            _output.WriteLine($"[{nameof(TestSubscribeCandlesWithDifferentIntervalsForSameInstrument)}] Подписка на свечи: {SubscriptionInterval.OneMinute} и {SubscriptionInterval.FiveMinutes}");
            await manager.SubscribeCandlesAsync(
                new[] { instrumentOneMinute },
                onCandle: c => { candlesReceived.Add(c.Interval); _output.WriteLine($"[свеча] интервал={c.Interval}"); return Task.CompletedTask; });
            await manager.SubscribeCandlesAsync(
                new[] { instrumentFiveMinutes },
                onCandle: c => { candlesReceived.Add(c.Interval); _output.WriteLine($"[свеча] интервал={c.Interval}"); return Task.CompletedTask; });

            await WaitUntilAsync(() => candlesReceived.Count >= 2,
                log: () => _output.WriteLine($"[{nameof(TestSubscribeCandlesWithDifferentIntervalsForSameInstrument)}] Ожидание свечей: {candlesReceived.Count}"));

            // Разные интервалы одного инструмента должны обрабатываться раздельно,
            // без дублирования свечи одного интервала в обработчик другого.
            _output.WriteLine($"[{nameof(TestSubscribeCandlesWithDifferentIntervalsForSameInstrument)}] Полученные интервалы: [{string.Join(", ", candlesReceived)}]");
            candlesReceived.Should().BeEquivalentTo(
                new[] { SubscriptionInterval.OneMinute, SubscriptionInterval.FiveMinutes });
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task TestSubscribeTradingStatusesDeliversToListener()
    {
        _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesDeliversToListener)}] Начало теста");
        MarketDataStreamMockService.Reset();

        // Пул подписок — 8 (по одному потребителю на каждый из 6 типов рыночных данных + запас).
        var (manager, _) = await CreateStartedManagerAsync(maxSubscriptionsPerStream: 8);
        try
        {
            var instrument = NewInstrument(figi: "FIGI_A", uid: "UID_A");
            var statusesReceived = new ConcurrentBag<string>();

            _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesDeliversToListener)}] Подписка на статусы: {instrument.Uid}");
            await manager.SubscribeTradingStatusesAsync(
                new[] { instrument },
                onTradingStatuses: s => { statusesReceived.Add(s.InstrumentUid); _output.WriteLine($"[статус] {s.InstrumentUid}"); return Task.CompletedTask; });

            await WaitUntilAsync(() => statusesReceived.Count >= 1,
                log: () => _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesDeliversToListener)}] Ожидание статуса: {statusesReceived.Count}"));

            _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesDeliversToListener)}] Получено статусов: [{string.Join(", ", statusesReceived)}]");
            statusesReceived.Should().ContainSingle().Which.Should().Be("UID_A");
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task TestSubscribeTradingStatusesWithSharedIntervalInstrumentStillDelivers()
    {
        _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesWithSharedIntervalInstrumentStillDelivers)}] Начало теста");
        MarketDataStreamMockService.Reset();

        var (manager, _) = await CreateStartedManagerAsync();
        try
        {
            // Инструмент создан «свечным» конструктором (с интервалом), как если бы объект
            // был переиспользован из SubscribeCandlesAsync, но подписка оформляется на статусы торгов.
            var instrument = NewInstrument(figi: "FIGI_A", uid: "UID_A", interval: SubscriptionInterval.OneMinute);
            var statusesReceived = new ConcurrentBag<string>();

            _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesWithSharedIntervalInstrumentStillDelivers)}] Подписка на статусы инструментом со «свечным» интервалом: {instrument.Uid}");
            await manager.SubscribeTradingStatusesAsync(
                new[] { instrument },
                onTradingStatuses: s => { statusesReceived.Add(s.InstrumentUid); _output.WriteLine($"[статус] {s.InstrumentUid}"); return Task.CompletedTask; });

            await WaitUntilAsync(() => statusesReceived.Count >= 1,
                log: () => _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesWithSharedIntervalInstrumentStillDelivers)}] Ожидание статуса: {statusesReceived.Count}"));

            // Несмотря на «свечной» конструктор инструмента, обновление статуса торгов доставляется responseHandler.
            _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesWithSharedIntervalInstrumentStillDelivers)}] Получено статусов: [{string.Join(", ", statusesReceived)}]");
            statusesReceived.Should().ContainSingle().Which.Should().Be("UID_A");
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task TestSubscribeTradingStatusesTwiceRegistersBothListeners()
    {
        _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesTwiceRegistersBothListeners)}] Начало теста");
        MarketDataStreamMockService.Reset();

        // Заглушка делает паузу 200 мс перед единственным пушем, чтобы оба responseHandler успели зарегистрироваться.
        MarketDataStreamMockService.TradingStatusPushDelayMs = 200;

        var (manager, _) = await CreateStartedManagerAsync();
        try
        {
            var instrument = NewInstrument(figi: "FIGI_A", uid: "UID_A");
            var statusesForListenerA = new ConcurrentBag<string>();
            var statusesForListenerB = new ConcurrentBag<string>();

            _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesTwiceRegistersBothListeners)}] Подписка №1 (слушатель A): {instrument.Uid}");
            await manager.SubscribeTradingStatusesAsync(
                new[] { instrument },
                onTradingStatuses: s => { statusesForListenerA.Add(s.InstrumentUid); _output.WriteLine($"[A] статус: {s.InstrumentUid}"); return Task.CompletedTask; });

            // Повторная подписка после активации первой; управление не открывает доступ к состоянию подписки,
            // поэтому активацию считаем состоявшейся после успешного завершения первой подписки.
            _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesTwiceRegistersBothListeners)}] Подписка №2 (слушатель B): {instrument.Uid}");
            await manager.SubscribeTradingStatusesAsync(
                new[] { instrument },
                onTradingStatuses: s => { statusesForListenerB.Add(s.InstrumentUid); _output.WriteLine($"[B] статус: {s.InstrumentUid}"); return Task.CompletedTask; });

            await WaitUntilAsync(() => statusesForListenerA.Count >= 1 && statusesForListenerB.Count >= 1,
                log: () => _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesTwiceRegistersBothListeners)}] Ожидание доставки: A={statusesForListenerA.Count}, B={statusesForListenerB.Count}"));

            _output.WriteLine($"[{nameof(TestSubscribeTradingStatusesTwiceRegistersBothListeners)}] Доставлено: A=[{string.Join(", ", statusesForListenerA)}], B=[{string.Join(", ", statusesForListenerB)}]");
            statusesForListenerA.Should().ContainSingle().Which.Should().Be("UID_A");
            statusesForListenerB.Should().ContainSingle().Which.Should().Be("UID_A");
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task TestSubscribeOrderBookWithAllTypeDeliversToListener()
    {
        _output.WriteLine($"[{nameof(TestSubscribeOrderBookWithAllTypeDeliversToListener)}] Начало теста");
        MarketDataStreamMockService.Reset();

        // Реальные пуши всегда приходят с конкретным типом, а не ALL.
        MarketDataStreamMockService.OrderBookPushType = OrderBookType.Exchange;

        var (manager, _) = await CreateStartedManagerAsync();
        try
        {
            var instrument = NewInstrument(figi: "FIGI_A", uid: "UID_A", depth: 10, orderBook: OrderBookType.All);
            var orderBooksReceived = new ConcurrentBag<string>();

            _output.WriteLine($"[{nameof(TestSubscribeOrderBookWithAllTypeDeliversToListener)}] Подписка на стакан (тип ALL, пуш Exchange): {instrument.Uid}");
            await manager.SubscribeOrderBookAsync(
                new[] { instrument },
                onOrderBook: ob => { orderBooksReceived.Add(ob.InstrumentUid); _output.WriteLine($"[стакан] {ob.InstrumentUid}"); return Task.CompletedTask; });

            await WaitUntilAsync(() => orderBooksReceived.Count >= 1,
                log: () => _output.WriteLine($"[{nameof(TestSubscribeOrderBookWithAllTypeDeliversToListener)}] Ожидание стакана: {orderBooksReceived.Count}"));

            // Стакан с типом EXCHANGE матчится с подпиской ALL и доставляется responseHandler.
            _output.WriteLine($"[{nameof(TestSubscribeOrderBookWithAllTypeDeliversToListener)}] Получено стаканов: [{string.Join(", ", orderBooksReceived)}]");
            orderBooksReceived.Should().ContainSingle().Which.Should().Be("UID_A");
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task TestSubscribeOrderBookWithExplicitTypeIgnoresOtherType()
    {
        _output.WriteLine($"[{nameof(TestSubscribeOrderBookWithExplicitTypeIgnoresOtherType)}] Начало теста");
        MarketDataStreamMockService.Reset();

        // Подписка на биржевой стакан, а заглушка шлёт стакан дилера.
        MarketDataStreamMockService.OrderBookPushType = OrderBookType.Dealer;

        var (manager, _) = await CreateStartedManagerAsync();
        try
        {
            var instrument = NewInstrument(figi: "FIGI_A", uid: "UID_A", depth: 10, orderBook: OrderBookType.Exchange);
            var orderBooksReceived = new ConcurrentBag<string>();

            _output.WriteLine($"[{nameof(TestSubscribeOrderBookWithExplicitTypeIgnoresOtherType)}] Подписка на стакан (тип Exchange, пуш Dealer): {instrument.Uid}");
            await manager.SubscribeOrderBookAsync(
                new[] { instrument },
                onOrderBook: ob => { orderBooksReceived.Add(ob.InstrumentUid); _output.WriteLine($"[стакан] {ob.InstrumentUid} (не должен был прийти)"); return Task.CompletedTask; });

            // Ждём достаточно времени, чтобы пуш гарантированно был обработан или отброшен (pollDelay 500 мс).
            _output.WriteLine($"[{nameof(TestSubscribeOrderBookWithExplicitTypeIgnoresOtherType)}] Ожидание 500 мс, чтобы пуш Dealer был обработан или отброшен...");
            await Task.Delay(500);

            _output.WriteLine($"[{nameof(TestSubscribeOrderBookWithExplicitTypeIgnoresOtherType)}] Получено стаканов (ожидаем 0): [{string.Join(", ", orderBooksReceived)}]");
            orderBooksReceived.Should().BeEmpty();
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    private async Task<(MarketDataStreamManager Manager, GrpcChannel Channel)> CreateStartedManagerAsync(
        int maxSubscriptionsPerStream = 8,
        int maxStreams = 2)
    {
        var channel = _fixture.CreateChannel();
        var streamClient = new MarketDataStreamService.MarketDataStreamServiceClient(channel);
        var marketDataClient = new MarketDataService.MarketDataServiceClient(channel);

        var configurer = new MarketDataManagerConfigurer(
            streamClient)
            .WithMaxSubscriptionsPerStream(maxSubscriptionsPerStream)
            .WithMaxStreams(maxStreams)
            .WithConnectTimeoutMs(5000)
            .WithPingDelayMs(500)
            .WithInactivityTimeoutMs(5000)
            .WithBackOffDelayMs(100);

        var manager = new MarketDataStreamManager(configurer, marketDataClient);
        await manager.StartAsync();
        return (manager, channel);
    }

    private static MarketDataInstrument NewInstrument(
        string figi,
        string uid,
        string tickerClassCode = "TICKER_CLASS",
        SubscriptionInterval? interval = null,
        int? depth = null,
        OrderBookType? orderBook = null)
        => new(
            figi: figi,
            tickerClassCode: tickerClassCode,
            uid: uid,
            interval: interval,
            depth: depth,
            orderBook: orderBook);

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000, int pollMs = 50, Action? log = null)
    {
        var sw = Stopwatch.StartNew();
        var lastLog = 0L;
        while (!condition())
        {
            // Троттлим лог, чтобы не спамить на каждый поллинг (лог не чаще раза в 250 мс).
            if (log != null && sw.ElapsedMilliseconds - lastLog >= 250)
            {
                lastLog = sw.ElapsedMilliseconds;
                log();
            }

            if (sw.ElapsedMilliseconds > timeoutMs)
            {
                log?.Invoke();
                throw new TimeoutException("Условие не выполнено в течение " + timeoutMs + " мс");
            }

            await Task.Delay(pollMs);
        }
    }
}