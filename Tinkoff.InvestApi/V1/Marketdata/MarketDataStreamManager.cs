using System.Collections.Concurrent;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.Stream.Marketdata.Models;
using Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;
using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi.V1.Marketdata;
using Tinkoff.InvestApi.V1.Marketdata.Models.Wrapper;
using Tinkoff.InvestApi.V1.Marketdata.Wrappers;

namespace Tinkoff.InvestApi.Stream.Marketdata;

/// <summary>
/// Менеджер для управления пулом MarketDataStreamWrapper.
/// Автоматически создаёт новые стримы при достижении лимита подписок на одном стриме
/// и выбрасывает ошибку при достижении лимита на количество стримов.
/// Все ответы от стримов маршрутизируются в общий <see cref="MarketDataStreamContext"/>.
/// </summary>
public class MarketDataStreamManager
{
    private readonly object _lock = new();
    private readonly List<MarketDataStreamWrapper> _streams = [];
    private readonly ConcurrentDictionary<string, MarketDataStreamWrapper> _instrumentToStream = new();
    private readonly int _maxSubscriptionsPerStream;
    private readonly int _maxStreams;
    private readonly MarketDataStreamContext _context = new();
    private readonly ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse> _configurer;
    private readonly MarketDataService.MarketDataServiceClient _client;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<MarketDataStreamManager>? _logger;
    private CancellationTokenSource _terminationCts = new();

    public MarketDataStreamManager(
        int maxSubscriptionsPerStream,
        int maxStreams,
        ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse> configurer,
        MarketDataService.MarketDataServiceClient client,
        ILoggerFactory? loggerFactory = null)
    {
        _maxSubscriptionsPerStream = maxSubscriptionsPerStream;
        _maxStreams = maxStreams;
        _configurer = configurer;
        _client = client;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<MarketDataStreamManager>();
    }

    /// <summary>
    /// Общий контекст, в который маршрутизируются ответы всех стримов.
    /// Через него регистрируются глобальные обработчики и читаются входящие ответы.
    /// </summary>
    public MarketDataStreamContext Context => _context;

    /// <summary>
    /// Текущее количество стримов в пуле.
    /// </summary>
    public int StreamCount
    {
        get
        {
            lock (_lock)
            {
                return _streams.Count;
            }
        }
    }

    /// <summary>
    /// Суммарное количество активных подписок по всем стримам.
    /// </summary>
    public int TotalSubscriptionCount
    {
        get
        {
            lock (_lock)
            {
                return _streams.Sum(s => s.GetSubscriptionCount());
            }
        }
    }

    /// <summary>
    /// Создаёт первый стрим и устанавливает соединение. Если стрим уже создан — не выполняет никаких действий.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        MarketDataStreamWrapper stream;
        lock (_lock)
        {
            if (_streams.Count > 0)
            {
                return;
            }

            stream = CreateStream();
            _streams.Add(stream);
        }

        await stream.ConnectAsync(cancellationToken);
    }

    /// <summary>
    /// Подписывается на свечи, распределяя инструменты между стримами пула.
    /// </summary>
    public Task SubscribeCandlesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteSubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKey(MarketDataResponseType.CANDLE, i.Figi, i.Uid),
            (s, batch, ct) => s.SubscribeCandlesAsync(batch, ct),
            cancellationToken);

    /// <summary>
    /// Отписывается от свечей, направляя запрос в стрим, удерживающий подписку.
    /// </summary>
    public Task UnsubscribeCandlesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteUnsubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKey(MarketDataResponseType.CANDLE, i.Figi, i.Uid),
            (s, batch, ct) => s.UnsubscribeCandlesAsync(batch, ct),
            cancellationToken);

    /// <summary>
    /// Подписывается на стаканы, распределяя инструменты между стримами пула.
    /// </summary>
    public Task SubscribeOrderBookAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteSubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKey(MarketDataResponseType.ORDER_BOOK, i.Figi, i.Uid),
            (s, batch, ct) => s.SubscribeOrderBookAsync(batch, ct),
            cancellationToken);

    /// <summary>
    /// Отписывается от стаканов, направляя запрос в стрим, удерживающий подписку.
    /// </summary>
    public Task UnsubscribeOrderBookAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteUnsubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKey(MarketDataResponseType.ORDER_BOOK, i.Figi, i.Uid),
            (s, batch, ct) => s.UnsubscribeOrderBookAsync(batch, ct),
            cancellationToken);

    /// <summary>
    /// Подписывается на сделки, распределяя инструменты между стримами пула.
    /// </summary>
    public Task SubscribeTradesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteSubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKey(MarketDataResponseType.TRADE, i.Figi, i.Uid),
            (s, batch, ct) => s.SubscribeTradesAsync(batch, ct),
            cancellationToken);

    /// <summary>
    /// Отписывается от сделок, направляя запрос в стрим, удерживающий подписку.
    /// </summary>
    public Task UnsubscribeTradesAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteUnsubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKey(MarketDataResponseType.TRADE, i.Figi, i.Uid),
            (s, batch, ct) => s.UnsubscribeTradesAsync(batch, ct),
            cancellationToken);

    /// <summary>
    /// Подписывается на цены последней сделки, распределяя инструменты между стримами пула.
    /// </summary>
    public Task SubscribeLastPriceAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteSubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKey(MarketDataResponseType.LAST_PRICE, i.Figi, i.Uid),
            (s, batch, ct) => s.SubscribeLastPriceAsync(batch, ct),
            cancellationToken);

    /// <summary>
    /// Отписывается от цен последней сделки, направляя запрос в стрим, удерживающий подписку.
    /// </summary>
    public Task UnsubscribeLastPriceAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteUnsubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKey(MarketDataResponseType.LAST_PRICE, i.Figi, i.Uid),
            (s, batch, ct) => s.UnsubscribeLastPriceAsync(batch, ct),
            cancellationToken);

    /// <summary>
    /// Подписывается на открытые позиции, распределяя инструменты между стримами пула.
    /// </summary>
    public Task SubscribeOpenInterestAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteSubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKey(MarketDataResponseType.OPEN_INTEREST, i.Figi, i.Uid),
            (s, batch, ct) => s.SubscribeOpenInterestAsync(batch, ct),
            cancellationToken);

    /// <summary>
    /// Отписывается от открытых позиций, направляя запрос в стрим, удерживающий подписку.
    /// </summary>
    public Task UnsubscribeOpenInterestAsync(IEnumerable<MarketDataInstrument> instruments, CancellationToken cancellationToken = default)
        => RouteUnsubscribeAsync(
            instruments.ToList(),
            i => SubscriptionKey(MarketDataResponseType.OPEN_INTEREST, i.Figi, i.Uid),
            (s, batch, ct) => s.UnsubscribeOpenInterestAsync(batch, ct),
            cancellationToken);

    private MarketDataStreamWrapper CreateStream()
    {
        return new MarketDataStreamWrapper(_configurer, RouteResponseAsync, RecoveringConnectionHandler, _loggerFactory);
    }

    /// <summary>
    /// Распределяет подписки между стримами пула, автоматически создавая новые стримы
    /// при заполнении текущих и выбрасывая ошибку при достижении лимита стримов.
    /// </summary>
    private async Task RouteSubscribeAsync<T>(
        IReadOnlyList<T> instruments,
        Func<T, string> keySelector,
        Func<MarketDataStreamWrapper, IReadOnlyList<T>, CancellationToken, Task> subscribeOnStream,
        CancellationToken cancellationToken)
    {
        if (instruments.Count == 0)
        {
            return;
        }

        var remaining = new Queue<T>(instruments);
        while (remaining.Count > 0)
        {
            int capacity = 0;
            MarketDataStreamWrapper? stream;
            lock (_lock)
            {
                stream = _streams.FirstOrDefault(s => (capacity = _maxSubscriptionsPerStream - s.GetSubscriptionCount() - s.GetPendingSubscriptionsCount()) > 0);
                if (stream == null)
                {
                    if (_streams.Count >= _maxStreams)
                    {
                        throw new InvalidOperationException($"Достигнут лимит количества стримов: {_maxStreams}. Увеличьте лимит или удалите неиспользуемые подписки.");
                    }

                    stream = CreateStream();
                    _streams.Add(stream);
                    capacity = _maxSubscriptionsPerStream;
                }
            }

            var batch = new List<T>();
            while (remaining.Count > 0 && batch.Count < capacity)
            {
                batch.Add(remaining.Dequeue());
            }

            await subscribeOnStream(stream, batch, cancellationToken);

            foreach (var item in batch)
            {
                _instrumentToStream[keySelector(item)] = stream;
            }
        }
    }

    /// <summary>
    /// Направляет отписку в стрим, удерживающий соответствующую подписку.
    /// </summary>
    private async Task RouteUnsubscribeAsync<T>(
        IReadOnlyList<T> instruments,
        Func<T, string> keySelector,
        Func<MarketDataStreamWrapper, IReadOnlyList<T>, CancellationToken, Task> unsubscribeOnStream,
        CancellationToken cancellationToken)
    {
        if (instruments.Count == 0)
        {
            return;
        }

        var byStream = new Dictionary<MarketDataStreamWrapper, List<T>>();
        foreach (var item in instruments)
        {
            var key = keySelector(item);
            if (_instrumentToStream.TryGetValue(key, out var stream))
            {
                if (!byStream.TryGetValue(stream, out var list))
                {
                    byStream[stream] = list = new List<T>();
                }

                list.Add(item);
            }
            else
            {
                _logger?.LogWarning("Не найдена подписка для отписки: {Key}", key);
            }
        }

        foreach (var kvp in byStream)
        {
            await unsubscribeOnStream(kvp.Key, kvp.Value, cancellationToken);

            foreach (var item in kvp.Value)
            {
                _instrumentToStream.TryRemove(keySelector(item), out _);
            }
        }
    }

    /// <summary>
    /// Маршрутизирует входящий ответ стрима в общий контекст.
    /// </summary>
    private async Task RouteResponseAsync(MarketDataResponse response)
    {
        if (response.Candle != null && response.Candle.IsInitialized())
        {
            await _context.PushCandleAsync(new CandleWrapper(response.Candle, IsHistorical: false));
        }
        else if (response.Trade != null && response.Trade.IsInitialized())
        {
            await _context.PushTradeAsync(new TradeWrapper(response.Trade));
        }
        else if (response.Orderbook != null && response.Orderbook.IsInitialized())
        {
            await _context.PushOrderBookAsync(new OrderBookWrapper(response.Orderbook));
        }
        else if (response.LastPrice != null && response.LastPrice.IsInitialized())
        {
            await _context.PushLastPriceAsync(new LastPriceWrapper(response.LastPrice));
        }
        else if (response.OpenInterest != null && response.OpenInterest.IsInitialized())
        {
            await _context.PushOpenInterestAsync(new OpenInterestWrapper(response.OpenInterest));
        }
        else if (response.TradingStatus != null && response.TradingStatus.IsInitialized())
        {
            await _context.PushTradingStatusAsync(new TradingStatusWrapper(response.TradingStatus));
        }
    }

    private async Task RecoveringConnectionHandler(StreamInfo streamInfo)
    {
        var stream = _streams.FirstOrDefault(s => s.Guid == streamInfo.Guid);
        if (stream != null)
        {
            var candleSubscriptions = stream.GetActiveSubscriptions
                .Where(s => s.Value is CandleSubscriptionRequest)
                .Select(s => s.Value)
                .Cast<CandleSubscriptionRequest>()
                .ToList();
            foreach (CandleSubscriptionRequest sub in candleSubscriptions)
            {
                await RecoverCandleSubscription(sub.Instruments[0], sub.LastInteractionTime, streamInfo.Guid);
            }
        }
    }

    private async Task RecoverCandleSubscription(MarketDataInstrument first, DateTime? lastInteractionTime, Guid guid)
    {
        _logger?.LogDebug("Recovering candle subscription for {Figi}...", first.Figi);
        var now = DateTime.UtcNow;
        int intervalNumber = (int)(first.Interval ?? SubscriptionInterval.Unspecified);
        var request = new GetCandlesRequest
        {
            InstrumentId = first.Uid,
            Interval = Enum.IsDefined(typeof(CandleInterval), intervalNumber) ? (CandleInterval)intervalNumber : CandleInterval.Unspecified,
            From = lastInteractionTime.ToTimestampOrNull(),
            To = now.ToTimestamp(),
        };
        var response = _client.GetCandles(request);
        var candles = response.Candles.Where(c => c.IsComplete)
            .Select(c => new CandleWrapper(MapCandle(c, first), IsHistorical: true))
            .ToList();
        candles.Sort((c1, c2) => c1.Time().Value.CompareTo(c2.Time().Value));
        candles.ForEach(async c =>
        {
            _logger?.LogDebug("Recovered candle: {Candle}", c);
            await _context.PushCandleAsync(c);  
        });
    }

    private static string SubscriptionKey(MarketDataResponseType responseType, string? figi, string? instrumentId)
    {
        return $"{responseType}:{figi}:{instrumentId}:";
    }

    private static Candle MapCandle(HistoricCandle candle, MarketDataInstrument instrument)
    {
        return new Candle
        {
            InstrumentUid = instrument.Uid,
            Figi = instrument.Figi,
            Ticker = instrument.TickerClassCode?.Split("_")[0],
            ClassCode = instrument.TickerClassCode?.Split("_")[1],
            Interval = instrument.Interval ?? SubscriptionInterval.Unspecified,
            Open = candle.Open,
            Close = candle.Close,
            High = candle.High,
            Low = candle.Low,
            Time = candle.Time,
            Volume = candle.Volume,
            VolumeBuy = candle.VolumeBuy,
            VolumeSell = candle.VolumeSell,
            CandleSourceType = candle.CandleSource,
        };
    }

    public async ValueTask DisposeAsync()
    {
        List<MarketDataStreamWrapper> streams;
        lock (_lock)
        {
            streams = _streams.ToList();
            _streams.Clear();
        }

        foreach (var stream in streams)
        {
            try
            {
                await stream.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Ошибка при освобождении стрима");
            }
        }

        _terminationCts.Cancel();
        _terminationCts.Dispose();
    }
}
