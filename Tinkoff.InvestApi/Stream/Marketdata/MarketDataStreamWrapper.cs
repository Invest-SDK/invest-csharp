using System.Collections.Concurrent;
using System.Threading.Channels;
using Google.Protobuf;
using Grpc.Core;
using Tinkoff.InvestApi.Stream.Marketdata.Models;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream.Marketdata;

public class MarketDataStreamWrapper
{
    private readonly Dictionary<MarketDataResponseType, HashSet<SubscriptionEntry>> _subscriptions = new Dictionary<MarketDataResponseType, HashSet<SubscriptionEntry>>()
    {
        { MarketDataResponseType.CANDLE, new HashSet<SubscriptionEntry>() },
        { MarketDataResponseType.LAST_PRICE, new HashSet<SubscriptionEntry>() },
        { MarketDataResponseType.ORDER_BOOK, new HashSet<SubscriptionEntry>() },
        { MarketDataResponseType.TRADE, new HashSet<SubscriptionEntry>() },
        { MarketDataResponseType.OPEN_INTEREST, new HashSet<SubscriptionEntry>() },
        { MarketDataResponseType.TRADING_STATUS, new HashSet<SubscriptionEntry>() },
        { MarketDataResponseType.OTHER, new HashSet<SubscriptionEntry>() }
    };
    private readonly ConcurrentDictionary<MarketDataResponseType, HashSet<Func<MarketDataResponse, Task>>> _messageHandlers = new();
    private ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse> _duplexStreamConfigurer;
    private ResilenceDuplexStreamWrapper<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>? _streamWrapper;
    private Channel<MarketDataSubscriptionResult> _channel = Channel.CreateUnbounded<MarketDataSubscriptionResult>(new UnboundedChannelOptions { SingleReader = true });

    public MarketDataStreamWrapper(ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse> duplexStreamConfigurer)
    {
        _duplexStreamConfigurer = duplexStreamConfigurer;
    }

    [Obsolete]
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        _streamWrapper ??= new ResilenceDuplexStreamWrapper<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>(
            streamConfigurer: _duplexStreamConfigurer,
            messageHandler: MessageHandler(),
            connectionHandler: ConnectionHandler()
            );
        await _streamWrapper.ConnectAsync(cancellationToken);
    }

    private async Task<MarketDataSubscriptionResult?> CallAsync(MarketDataRequest request)
    {
        await _streamWrapper!.CallAsync(request);
        var response = await _channel.Reader.ReadAsync();
        return response;
    }

    public void AddCandleHandler(Func<MarketDataResponse, Task> handler)
    {
        if (_messageHandlers[MarketDataResponseType.CANDLE] == null)
        {
            _messageHandlers[MarketDataResponseType.CANDLE] = new HashSet<Func<MarketDataResponse, Task>>();
        }
        _messageHandlers[MarketDataResponseType.CANDLE].Add(handler);
    }

    public void AddLastPriceHandler(Func<MarketDataResponse, Task> handler)
    {
        if (_messageHandlers[MarketDataResponseType.LAST_PRICE] == null)
        {
            _messageHandlers[MarketDataResponseType.LAST_PRICE] = new HashSet<Func<MarketDataResponse, Task>>();
        }
        _messageHandlers[MarketDataResponseType.LAST_PRICE].Add(handler);
    }

    public void AddOrderBookHandler(Func<MarketDataResponse, Task> handler)
    {
        if (_messageHandlers[MarketDataResponseType.ORDER_BOOK] == null)
        {
            _messageHandlers[MarketDataResponseType.ORDER_BOOK] = new HashSet<Func<MarketDataResponse, Task>>();
        }
        _messageHandlers[MarketDataResponseType.ORDER_BOOK].Add(handler);
    }

    public void AddTradeHandler(Func<MarketDataResponse, Task> handler)
    {
        if (_messageHandlers[MarketDataResponseType.TRADE] == null)
        {
            _messageHandlers[MarketDataResponseType.TRADE] = new HashSet<Func<MarketDataResponse, Task>>();
        }
        _messageHandlers[MarketDataResponseType.TRADE].Add(handler);
    }

    public void AddOpenInterestHandler(Func<MarketDataResponse, Task> handler)
    {
        if (_messageHandlers[MarketDataResponseType.OPEN_INTEREST] == null)
        {
            _messageHandlers[MarketDataResponseType.OPEN_INTEREST] = new HashSet<Func<MarketDataResponse, Task>>();
        }
        _messageHandlers[MarketDataResponseType.OPEN_INTEREST].Add(handler);
    }

    public void AddTradingStatusHandler(Func<MarketDataResponse, Task> handler)
    {
        if (_messageHandlers[MarketDataResponseType.TRADING_STATUS] == null)
        {
            _messageHandlers[MarketDataResponseType.TRADING_STATUS] = new HashSet<Func<MarketDataResponse, Task>>();
        }
        _messageHandlers[MarketDataResponseType.TRADING_STATUS].Add(handler);
    }

    public int GetSubscriptionCount()
    {
        return _subscriptions.Values.Sum(subscriptions => subscriptions.Count);
    }

    public async Task<MarketDataSubscriptionResult?> SubscribeAsync(
        MarketDataResponseType responseType,
        MarketDataInstrument instrument,
        CancellationToken cancellationToken = default)
    {
        await ConnectAsync(cancellationToken);

        var request = CreateSubscribeRequest(responseType, [new SubscriptionEntry
        {
            Instrument = instrument,
            ResponseType = responseType,
            IsActive = true
        }]);

        if (request == null)
        {
            return null;
        }

        return await CallAsync(request);
    }

    [Obsolete]
    public async Task<MarketDataSubscriptionResult?> UnsubscribeAsync(
        MarketDataResponseType responseType,
        MarketDataInstrument instrument,
        CancellationToken cancellationToken = default)
    {
        await ConnectAsync(cancellationToken);

        var request = CreateUnsubscribeRequest(responseType, [new SubscriptionEntry
        {
            Instrument = instrument,
            ResponseType = responseType,
            IsActive = true
        }]);

        if (request == null)
        {
            return null;
        }

        return await CallAsync(request);
    }

    public async ValueTask DisposeAsync()
    {
        if (_streamWrapper != null)
        {
            await _streamWrapper.DisposeAsync();
        }
    }

    private Func<MarketDataResponse, Task> MessageHandler()
    {
        return async response =>
        {
            var subscriptionResult = MarketDataUtils.GetSubscriptionResultFromResponse(response);
            
            if (subscriptionResult != null)
            {
                await _channel.Writer.WriteAsync(subscriptionResult);
                
                if (subscriptionResult.Action == MarketDataAction.SUBSCRIBE)
                {
                    foreach (var instrument in subscriptionResult.SubsciptionStatusDictionary.Keys)
                    {
                        var entry = new SubscriptionEntry
                        {
                            Instrument = instrument,
                            LastReceiveTime = DateTime.UtcNow,
                            IsActive = true,
                            ResponseType = subscriptionResult.ResponseType
                        };
                        _subscriptions[subscriptionResult.ResponseType].Add(entry);
                    }
                }
                else if (subscriptionResult.Action == MarketDataAction.UNSUBSCRIBE)
                {
                    foreach (var instrument in subscriptionResult.SubsciptionStatusDictionary.Keys)
                    {
                        var entry = _subscriptions[subscriptionResult.ResponseType]
                            .FirstOrDefault(e => e.Instrument != null && 
                                                 e.Instrument.Figi == instrument.Figi &&
                                                 e.Instrument.Uid == instrument.Uid);
                        if (entry != null)
                        {
                            _subscriptions[subscriptionResult.ResponseType].Remove(entry);
                        }
                    }
                }
                
                return;
            }

            var responseType = GetResponseTypeFromResponse(response);
            if (responseType != MarketDataResponseType.OTHER && _messageHandlers.TryGetValue(responseType, out var handlers))
            {
                List<Task> tasks = new List<Task>();
                foreach (var handler in handlers)
                {
                    tasks.Add(handler(response));
                }
                await Task.WhenAll(tasks);
            }
        };
    }

    [Obsolete]
    private Func<AsyncDuplexStreamingCall<MarketDataRequest, MarketDataResponse>, Task> ConnectionHandler()
    {
        return async stream =>
        {
            foreach (var kvp in _subscriptions)
            {
                var responseType = kvp.Key;
                var entries = kvp.Value.Where(e => e.IsActive && e.Instrument != null).ToList();
                
                if (entries.Count == 0)
                {
                    continue;
                }

                var request = CreateSubscribeRequest(responseType, entries);
                if (request != null)
                {
                    await stream.RequestStream.WriteAsync(request);
                }
            }
        };
    }

    private static MarketDataResponseType GetResponseTypeFromResponse(MarketDataResponse response)
    {
        if (response.Candle.IsInitialized()) return MarketDataResponseType.CANDLE;
        if (response.LastPrice.IsInitialized()) return MarketDataResponseType.LAST_PRICE;
        if (response.Orderbook.IsInitialized()) return MarketDataResponseType.ORDER_BOOK;
        if (response.Trade.IsInitialized()) return MarketDataResponseType.TRADE;
        if (response.OpenInterest.IsInitialized()) return MarketDataResponseType.OPEN_INTEREST;
        if (response.TradingStatus.IsInitialized()) return MarketDataResponseType.TRADING_STATUS;
        return MarketDataResponseType.OTHER;
    }

    [Obsolete]
    private static MarketDataRequest? CreateSubscribeRequest(MarketDataResponseType responseType, List<SubscriptionEntry> entries)
    {
        return responseType switch
        {
            MarketDataResponseType.CANDLE => new MarketDataRequest
            {
                SubscribeCandlesRequest = new SubscribeCandlesRequest
                {
                    SubscriptionAction = SubscriptionAction.Subscribe,
                    Instruments = { entries.Select(e => new CandleInstrument 
                    { 
                        Figi = e.Instrument!.Figi,
                        Interval = e.Instrument.Interval ?? SubscriptionInterval.OneMinute
                    }) }
                }
            },
            MarketDataResponseType.LAST_PRICE => new MarketDataRequest
            {
                SubscribeLastPriceRequest = new SubscribeLastPriceRequest
                {
                    SubscriptionAction = SubscriptionAction.Subscribe,
                    Instruments = { entries.Select(e => new LastPriceInstrument 
                    { 
                        Figi = e.Instrument!.Figi
                    }) }
                }
            },
            MarketDataResponseType.ORDER_BOOK => new MarketDataRequest
            {
                SubscribeOrderBookRequest = new SubscribeOrderBookRequest
                {
                    SubscriptionAction = SubscriptionAction.Subscribe,
                    Instruments = { entries.Select(e => new OrderBookInstrument 
                    { 
                        Figi = e.Instrument!.Figi,
                        Depth = e.Instrument.Depth ?? 1
                    }) }
                }
            },
            MarketDataResponseType.TRADE => new MarketDataRequest
            {
                SubscribeTradesRequest = new SubscribeTradesRequest
                {
                    SubscriptionAction = SubscriptionAction.Subscribe,
                    Instruments = { entries.Select(e => new TradeInstrument 
                    { 
                        Figi = e.Instrument!.Figi
                    }) }
                }
            },
            MarketDataResponseType.OPEN_INTEREST => new MarketDataRequest
            {
                SubscribeInfoRequest = new SubscribeInfoRequest
                {
                    SubscriptionAction = SubscriptionAction.Subscribe,
                    Instruments = { entries.Select(e => new InfoInstrument 
                    { 
                        Figi = e.Instrument!.Figi
                    }) }
                }
            },
            _ => null
        };
    }

    [Obsolete]
    private static MarketDataRequest? CreateUnsubscribeRequest(MarketDataResponseType responseType, List<SubscriptionEntry> entries)
    {
        return responseType switch
        {
            MarketDataResponseType.CANDLE => new MarketDataRequest
            {
                SubscribeCandlesRequest = new SubscribeCandlesRequest
                {
                    SubscriptionAction = SubscriptionAction.Unsubscribe,
                    Instruments = { entries.Select(e => new CandleInstrument
                    {
                        Figi = e.Instrument!.Figi,
                        Interval = e.Instrument.Interval ?? SubscriptionInterval.OneMinute
                    }) }
                }
            },
            MarketDataResponseType.LAST_PRICE => new MarketDataRequest
            {
                SubscribeLastPriceRequest = new SubscribeLastPriceRequest
                {
                    SubscriptionAction = SubscriptionAction.Unsubscribe,
                    Instruments = { entries.Select(e => new LastPriceInstrument
                    {
                        Figi = e.Instrument!.Figi
                    }) }
                }
            },
            MarketDataResponseType.ORDER_BOOK => new MarketDataRequest
            {
                SubscribeOrderBookRequest = new SubscribeOrderBookRequest
                {
                    SubscriptionAction = SubscriptionAction.Unsubscribe,
                    Instruments = { entries.Select(e => new OrderBookInstrument
                    {
                        Figi = e.Instrument!.Figi,
                        Depth = e.Instrument.Depth ?? 1
                    }) }
                }
            },
            MarketDataResponseType.TRADE => new MarketDataRequest
            {
                SubscribeTradesRequest = new SubscribeTradesRequest
                {
                    SubscriptionAction = SubscriptionAction.Unsubscribe,
                    Instruments = { entries.Select(e => new TradeInstrument
                    {
                        Figi = e.Instrument!.Figi
                    }) }
                }
            },
            MarketDataResponseType.OPEN_INTEREST => new MarketDataRequest
            {
                SubscribeInfoRequest = new SubscribeInfoRequest
                {
                    SubscriptionAction = SubscriptionAction.Unsubscribe,
                    Instruments = { entries.Select(e => new InfoInstrument
                    {
                        Figi = e.Instrument!.Figi
                    }) }
                }
            },
            _ => null
        };
    }

    public class SubscriptionEntry
    {
        public MarketDataInstrument? Instrument { get; set; }
        public DateTime? LastReceiveTime { get; set; }
        public bool IsActive { get; set; }
        public MarketDataResponseType ResponseType { get; set; }
    }
}
