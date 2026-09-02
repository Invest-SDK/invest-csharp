using Google.Protobuf.WellKnownTypes;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;

public record CandleWrapper(Candle Response, bool IsHistorical) : ResponseWrapper<Candle>(Response)
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
    /// Метод для получения интервала свечи
    /// </summary>
    public SubscriptionInterval Interval()
    {
        return Response.Interval;
    }

    /// <summary>
    /// Метод для полуения цены открытия за 1 инструмент
    /// </summary>
    public decimal Open()
    {
        return Response.Open;
    }

    /// <summary>
    /// Метод для полуения максимальной цены за 1 инструмент
    /// </summary>
    public decimal High()
    {
        return Response.High;
    }

    /// <summary>
    /// Метод для полуения минимальной цены за 1 инструмент
    /// </summary>
    public decimal Low()
    {
        return Response.Low;
    }

    /// <summary>
    /// Метод для полуения цены закрытия за 1 инструмент
    /// </summary>
    public decimal Close()
    {
        return Response.Close;
    }

    /// <summary>
    /// Метод для получения объёма сделок в лотах
    /// </summary>
    public long Volume()
    {
        return Response.Volume;
    }

    /// <summary>
    /// Метод для получения объёма сделок в лотах на покупку
    /// </summary>
    public long VolumeBuy()
    {
        return Response.VolumeBuy;
    }

    /// <summary>
    /// Метод для получения объёма сделок в лотах на продажу
    /// </summary>
    public long VolumeSell()
    {
        return Response.VolumeSell;
    }

    /// <summary>
    /// Мето для получения времени начала интервала свечи по UTC
    /// </summary>
    public DateTime? Time()
    {
        return TimestampExtensions.ToDateTimeOrNull(Response.Time);
    }

    /// <summary>
    /// Метод для получения времени последней сделки, вошедшей в свечу по UTC
    /// </summary>
    public DateTime? LastTradeTime()
    {
        return TimestampExtensions.ToDateTimeOrNull(Response.LastTradeTs);
    }

    /// <summary>
    /// Метод для получения UID инструмента
    /// </summary>
    public String InstrumentUid()
    {
        return Response.InstrumentUid;
    }

    /// <summary>
    /// Метод для получения типа источника свечей
    /// </summary>
    public CandleSource CandleSourceType()
    {
        return Response.CandleSourceType;
    }
}
