using Google.Protobuf.WellKnownTypes;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;

public record LastPriceWrapper(LastPrice Response) : ResponseWrapper<LastPrice>(Response)
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
    /// Метод для получения цены инструмента
    /// </summary>
    public decimal Price()
    {
        return Response.Price;
    }

    /// <summary>
    /// Метод для получения времени последней цены по UTC по времени биржи
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
    /// Метод для получения типа последней цены
    /// </summary>
    public LastPriceType Type()
    {
        return Response.LastPriceType;
    }
}
