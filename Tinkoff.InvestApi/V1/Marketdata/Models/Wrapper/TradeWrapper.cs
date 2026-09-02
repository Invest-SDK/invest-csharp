using Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;

namespace Tinkoff.InvestApi.V1.Marketdata.Models.Wrapper;

public record TradeWrapper(Trade Response) : ResponseWrapper<Trade>(Response)
{
    
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
    /// Метод для получения направления сделки
    /// </summary>
    public TradeDirection Direction()
    {
        return Response.Direction;
    }

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

    /// <summary>
    /// Метод для получения типа источника сделок
    /// </summary>
    public TradeSourceType TradeSource()
    {
        return Response.TradeSource;
    }
}
