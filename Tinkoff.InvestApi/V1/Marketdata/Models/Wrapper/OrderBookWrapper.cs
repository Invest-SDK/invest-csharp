using Google.Protobuf.WellKnownTypes;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;

public record OrderBookWrapper(OrderBook Response) : ResponseWrapper<OrderBook>(Response)
{
    private readonly List<OrderWrapper> bids = [.. Response.Bids.Select(x => new OrderWrapper(x))];
    private readonly List<OrderWrapper> asks = [.. Response.Asks.Select(x => new OrderWrapper(x))];

    /// <summary>
    /// Метод для получения FIGI-идентификатора инструмента
    /// </summary>
    public String Figi()
    {
        return Response.Figi;
    }

    /// <summary>
    /// Метод для получения тикера инструмента
    /// </summary>
    public String Ticker()
    {
        return Response.Ticker;
    }

    /// <summary>
    /// Метод для получения класс-кода инструмента
    /// </summary>
    public String ClassCode()
    {
        return Response.ClassCode;
    }

    /// <summary>
    /// Метод для получения глубины стакана
    /// </summary>
    public int Depth()
    {
        return Response.Depth;
    }

    /// <summary>
    /// Метод для флага консистентности стакана
    /// </summary>
    public bool IsConsistent()
    {
        return Response.IsConsistent;
    }

    /// <summary>
    /// Метод для получения времени последнего обновления стакана по UTC
    /// </summary>
    public DateTime? Time()
    {
        return TimestampExtensions.ToDateTimeOrNull(Response.Time);
    }

    /// <summary>
    /// Метод для получения UID инструмента
    /// </summary>
    public String InstrumentUid()
    {
        return Response.InstrumentUid;
    }

    public record OrderWrapper(Order Response) : ResponseWrapper<Order>(Response)
    {
        /// <summary>
        /// Метод для получения цены
        /// </summary>
        public decimal Price()
        {
            return Response.Price;
        }

        /// <summary>
        /// Метод для получения количества лотов
        /// </summary>
        public long Quantity()
        {
            return Response.Quantity;
        }
    } 
}
