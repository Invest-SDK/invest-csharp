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
    private ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse> _duplexStreamConfigurer;
    private ResilenceDuplexStreamWrapper<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse>? _streamWrapper;
    private Channel<MarketDataResponse> _channel = Channel.CreateUnbounded<MarketDataResponse>(new UnboundedChannelOptions { SingleReader = true });

    public MarketDataStreamWrapper(ResilenceDuplexStreamConfigurer<MarketDataStreamService.MarketDataStreamServiceClient, MarketDataRequest, MarketDataResponse> duplexStreamConfigurer)
    {
        _duplexStreamConfigurer = duplexStreamConfigurer;
    }

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
        return MarketDataUtils.GetSubscriptionResultFromResponse(response);
    }

    private Func<MarketDataResponse, Task> MessageHandler()
    {
        return async (MarketDataResponse response) =>
        {
            if (response.Candle.IsInitialized())
            {
                
            }
            await _channel.Writer.WriteAsync(response);
        };
    }

    private Func<AsyncDuplexStreamingCall<MarketDataRequest, MarketDataResponse>, Task> ConnectionHandler()
    {
        throw new NotImplementedException();
    }

    public class SubscriptionEntry
    {

    }
}
