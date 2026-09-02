using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;

public record OpenInterestWrapper(OpenInterest Response) : ResponseWrapper<OpenInterest>(Response)
{

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

    public long OpenInterest()
    {
        return Response.OpenInterest_;
    }

    /// <summary>
    /// Метод для получения UID инструмента
    /// </summary>
    public String InstrumentUid()
    {
        return Response.InstrumentUid;
    }
}
