using System.Collections.Concurrent;
using System.Threading.Channels;
using Tinkoff.InvestApi.Stream.Marketdata.Models;
using Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;
using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi.V1.Marketdata;

namespace Tinkoff.InvestApi.Stream.Marketdata;

/// <summary>
/// Менеджер для управления пулом MarketDataStreamWrapper.
/// Автоматически создаёт новые стримы при достижении лимита подписок на одном стриме.
/// </summary>
public class MarketDataStreamManager
{

    private readonly ConcurrentQueue<MarketDataStreamWrapper> _streamPool = new();
    private readonly int _maxSubscriptionsPerStream;
    private MarketDataStreamContext _context;
    private CancellationTokenSource _terminationCts = new();

    public MarketDataStreamManager(int maxSubscriptionsPerStream)
    {
        _maxSubscriptionsPerStream = maxSubscriptionsPerStream;
        _context = new MarketDataStreamContext();
    }

    public void start()
    {

    }

}
