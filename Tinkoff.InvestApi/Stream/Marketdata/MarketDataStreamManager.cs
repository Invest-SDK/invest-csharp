using Tinkoff.InvestApi.Stream.Marketdata.Models;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream.Marketdata;

/// <summary>
/// Менеджер для управления пулом MarketDataStreamWrapper.
/// Автоматически создаёт новые стримы при достижении лимита подписок на одном стриме.
/// </summary>
public class MarketDataStreamManager
{
    private readonly List<MarketDataStreamWrapper> _streams = [];
    private readonly int _maxSubscriptionsPerStream;
    private readonly Func<MarketDataStreamWrapper> _streamFactory;
    private readonly object _lock = new();

    public MarketDataStreamManager(int maxSubscriptionsPerStream, Func<MarketDataStreamWrapper> streamFactory)
    {
        _maxSubscriptionsPerStream = maxSubscriptionsPerStream;
        _streamFactory = streamFactory;

        _streams.Add(_streamFactory());
    }

    public int MaxSubscriptionsPerStream => _maxSubscriptionsPerStream;

    public IReadOnlyList<MarketDataStreamWrapper> Streams
    {
        get
        {
            lock (_lock)
            {
                return _streams.ToList().AsReadOnly();
            }
        }
    }

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

    public MarketDataStreamWrapper GetAvailableStream()
    {
        lock (_lock)
        {
            var stream = _streams.FirstOrDefault(s => s.GetSubscriptionCount() < _maxSubscriptionsPerStream);

            if (stream == null)
            {
                stream = _streamFactory();
                _streams.Add(stream);
            }

            return stream;
        }
    }

    public MarketDataStreamWrapper GetStreamForSubscription(MarketDataResponseType responseType, MarketDataInstrument instrument)
    {
        lock (_lock)
        {
            var stream = _streams.FirstOrDefault(s =>
                s.GetSubscriptionCount() < _maxSubscriptionsPerStream &&
                HasSubscription(s, responseType, instrument));

            if (stream != null)
            {
                return stream;
            }

            return GetAvailableStream();
        }
    }

    public async Task<MarketDataSubscriptionResult?> SubscribeAsync(
        MarketDataResponseType responseType,
        MarketDataInstrument instrument,
        CancellationToken cancellationToken = default)
    {
        var stream = GetStreamForSubscription(responseType, instrument);
        return await stream.SubscribeAsync(responseType, instrument, cancellationToken);
    }

    [Obsolete]
    public async Task<MarketDataSubscriptionResult?> UnsubscribeAsync(
        MarketDataResponseType responseType,
        MarketDataInstrument instrument,
        CancellationToken cancellationToken = default)
    {
        MarketDataStreamWrapper? stream;

        lock (_lock)
        {
            stream = _streams.FirstOrDefault(s => HasSubscription(s, responseType, instrument));
        }

        if (stream == null)
        {
            return null;
        }

        return await stream.UnsubscribeAsync(responseType, instrument, cancellationToken);
    }

    public async Task RemoveEmptyStreamsAsync(CancellationToken cancellationToken = default)
    {
        List<MarketDataStreamWrapper> streamsToRemove;

        lock (_lock)
        {
            streamsToRemove = _streams.Where(s => s.GetSubscriptionCount() == 0).ToList();

            if (_streams.Count - streamsToRemove.Count <= 1)
            {
                var streamsToKeep = 1 - (_streams.Count - streamsToRemove.Count);
                streamsToRemove = streamsToRemove.SkipLast(streamsToKeep).ToList();
            }

            _streams.RemoveAll(s => streamsToRemove.Contains(s));
        }

        foreach (var stream in streamsToRemove)
        {
            try
            {
                await stream.DisposeAsync();
            }
            catch
            {
                // Игнорируем ошибки при удалении пустых стримов
            }
        }
    }

    public void AddCandleHandler(Func<MarketDataResponse, Task> handler)
    {
        lock (_lock)
        {
            foreach (var stream in _streams)
            {
                stream.AddCandleHandler(handler);
            }
        }
    }

    public void AddLastPriceHandler(Func<MarketDataResponse, Task> handler)
    {
        lock (_lock)
        {
            foreach (var stream in _streams)
            {
                stream.AddLastPriceHandler(handler);
            }
        }
    }

    public void AddOrderBookHandler(Func<MarketDataResponse, Task> handler)
    {
        lock (_lock)
        {
            foreach (var stream in _streams)
            {
                stream.AddOrderBookHandler(handler);
            }
        }
    }

    public void AddTradeHandler(Func<MarketDataResponse, Task> handler)
    {
        lock (_lock)
        {
            foreach (var stream in _streams)
            {
                stream.AddTradeHandler(handler);
            }
        }
    }

    public void AddOpenInterestHandler(Func<MarketDataResponse, Task> handler)
    {
        lock (_lock)
        {
            foreach (var stream in _streams)
            {
                stream.AddOpenInterestHandler(handler);
            }
        }
    }

    public void AddTradingStatusHandler(Func<MarketDataResponse, Task> handler)
    {
        lock (_lock)
        {
            foreach (var stream in _streams)
            {
                stream.AddTradingStatusHandler(handler);
            }
        }
    }

    public async Task ConnectAllStreamsAsync(CancellationToken cancellationToken = default)
    {
        List<MarketDataStreamWrapper> streams;

        lock (_lock)
        {
            streams = _streams.ToList();
        }

        var tasks = streams.Select(s => s.ConnectAsync(cancellationToken)).ToList();
        await Task.WhenAll(tasks);
    }

    public async Task DisposeAsync()
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
            catch
            {
                // Игнорируем ошибки при disposal
            }
        }
    }

    private static bool HasSubscription(MarketDataStreamWrapper stream, MarketDataResponseType responseType, MarketDataInstrument instrument)
    {
        return stream.GetSubscriptionCount() > 0;
    }
}
