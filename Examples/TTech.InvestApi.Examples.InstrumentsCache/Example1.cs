
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace TTech.InvestApi.Examples.InstrumentsCache;

public class Example1(ILogger<Example1> logger, InvestApiClient client)
{
    public async Task Run(CancellationToken cancellationToken)
    {
        // Запрос инструмента по значению UID
        // Первый запрос выполнится с обращением к InvestAPI
        var requestByUid = new InstrumentRequest
        {
            IdType = InstrumentIdType.Uid,
            ClassCode = "",
            Id = "962e2a95-02a9-4171-abd7-aa198dbe643a"
        };
        var instrumentByUid = await client.Instruments.ShareBy(requestByUid, cancellationToken);

        // Запрос инструмента по значению FIGI
        // Этот и последующие запросы извлекут данные из кэша
        var requestByFigi = new InstrumentRequest
        {
            IdType = InstrumentIdType.Figi,
            ClassCode = "",
            Id = "BBG004730RP0"
        };
        var instrumentByFigi = await client.Instruments.ShareBy(requestByFigi, cancellationToken);

        // Запрос инструмента по значениям TICKER+CLASSCODE
        // Этот и последующие запросы извлекут данные из кэша
        var requestByTicker = new InstrumentRequest
        {
            IdType = InstrumentIdType.Ticker,
            ClassCode = "TQBR",
            Id = "GAZP"
        };
        var instrumentByTicker = await client.Instruments.ShareBy(requestByTicker, cancellationToken);

        // Запрос инструмента по значению POSITION_UID
        // Этот запрос извлекает данные из кэша
        var requestByPositionUid = new InstrumentRequest
        {
            IdType = InstrumentIdType.PositionUid,
            ClassCode = "",
            Id = "4f9d0c81-cdf9-4735-8295-bacbfa3b8a51"
        };
        var instrumentByPositionUid = await client.Instruments.ShareBy(requestByPositionUid, cancellationToken);

        logger.LogInformation("Instrument by uid: {0}", instrumentByUid.Uid);
        logger.LogInformation("Instrument by figi: {0}", instrumentByFigi.Uid);
        logger.LogInformation("Instrument by ticker: {0}", instrumentByTicker.Uid);
        logger.LogInformation("Instrument by position uid: {0}", instrumentByPositionUid.Uid);
    }
}
