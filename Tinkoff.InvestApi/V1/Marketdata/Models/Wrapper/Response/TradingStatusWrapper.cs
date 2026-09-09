using Google.Protobuf.WellKnownTypes;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;

public record TradingStatusWrapper(TradingStatus Response) : ResponseWrapper<TradingStatus>(Response)
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
    /// Метод для получения статуса торговли инструментом
    /// </summary>
    public SecurityTradingStatus TradingStatus()
    {
        return Response.TradingStatus_;
    }

    /// <summary>
    /// Метод для получения признака доступности выставления лимитной заявки по инструменту
    /// </summary>
    public bool LimitOrderAvailableFlag()
    {
        return Response.LimitOrderAvailableFlag;
    }

    /// <summary>
    /// Метод для получения признака доступности выставления рыночной заявки по инструменту
    /// </summary>
    public bool MarketOrderAvailableFlag()
    {
        return Response.MarketOrderAvailableFlag;
    }

    /// <summary>
    /// Метод для получения времени изменения торгового статуса по UTC
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
}
