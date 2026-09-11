using Grpc.Core;

namespace Tinkoff.InvestApi.V1.Marketdata.Models;

public record StreamInfo(Guid Guid, AsyncDuplexStreamingCall<MarketDataRequest, MarketDataResponse> StreamingCall)
{
    
}
