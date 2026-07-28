#### О примере

Пример демонстрирует возможность кэширования справочных данных по инструментам. Инструмент может иметь множественную идентификацию. Получение данных одним из способов идентификации позволяет в последствии извлечь те же данные на основе другого поддерживаемого способа без выполнения запроса к InvestAPI.

#### Принцип работы

1. Включение опции кэширования
2. Выполнение запроса к InvestAPI для получения данных по иструменту
3. Сохранение данных в in-memory кэше на указанное время
4. Повторное выполнение запроса для получения данных по иструменту извлекает данные из кэша, если они есть, в противном случае выполняет запрос к InvestAPI

#### Множественная идентификация

Кэширование распространяется на инструменты, получаемые с помощью методов `InstrumentsServiceClient` с указанием способов идентификации:

|Модель|Метод|Способы идентификации|
|------|-----|---------------------|
|Bond|BondBy()|Figi, PositionUid, Ticker, Uid|
|Currency|CurrencyBy()|Figi, PositionUid, Ticker, Uid|
|DfaResponse|DfaBy()|PositionUid, Uid|
|Etf|EtfBy()|Figi, PositionUid, Ticker, Uid|
|Future|FutureBy()|Figi, PositionUid, Ticker, Uid|
|Instrument|GetInstrumentBy()|Figi, PositionUid, Ticker, Uid|
|Option|OptionBy()|PositionUid, Ticker, Uid|
|Share|ShareBy()|Figi, PositionUid, Ticker, Uid|
|StructuredNote|StructuredNoteBy()|Figi, PositionUid, Ticker, Uid|

#### Как запустить

1. В файле конфигурации указать необходимые параметры кэширования в `appsettings.json`, например
   
    ```json
    {
      ...
      "Caching": {
        "Enabled": true,
        "AbsoluteExpirationRelativeToNowMinutes": 20,
        "SlidingExpirationMinutes": 5
      }
      ...
    }
    ```

    Значения по умолчанию:
    
    - Enabled = true
    - AbsoluteExpirationRelativeToNowMinutes = 60
    - SlidingExpirationMinutes = 20

2. Пример получения данных по инструменту с использованием разных способов идентификации

    Инструмент
    ```json
    {
      "instrument": {
        ...
        "figi": "BBG004730RP0",
        "ticker": "GAZP",
        "classCode": "TQBR",
        "name": "Газпром",
        "uid": "962e2a95-02a9-4171-abd7-aa198dbe643a",
        "positionUid": "4f9d0c81-cdf9-4735-8295-bacbfa3b8a51",
        ...
      }
    }
    ```

    Пример кода
    ```csharp
    // Запрос инструмента по значению UID
    // Первый запрос выполнится с обращением к InvestAPI
    var requestByUid = new InstrumentRequest
    {
        IdType = InstrumentIdType.Uid,
        ClassCode = "",
        Id = "962e2a95-02a9-4171-abd7-aa198dbe643a"
    };
    var instrumentByUid = await client.Instruments.ShareBy(requestByUid, CancellationToken.None);

    // Запрос инструмента по значению FIGI
    // Этот и последующие запросы извлекут данные из кэша
    var requestByFigi = new InstrumentRequest
    {
        IdType = InstrumentIdType.Figi,
        ClassCode = "",
        Id = "BBG004730RP0"
    };
    var instrumentByFigi = await client.Instruments.ShareBy(requestByFigi, CancellationToken.None);

    // Запрос инструмента по значениям TICKER+CLASSCODE
    // Этот и последующие запросы извлекут данные из кэша
    var requestByTicker = new InstrumentRequest
    {
        IdType = InstrumentIdType.Ticker,
        ClassCode = "TQBR",
        Id = "GAZP"
    };
    var instrumentByTicker = await client.Instruments.ShareBy(requestByTicker, CancellationToken.None);

    // Запрос инструмента по значению POSITION_UID
    // Этот запрос извлекает данные из кэша
    var requestByPositionUid = new InstrumentRequest
    {
        IdType = InstrumentIdType.PositionUid,
        ClassCode = "",
        Id = "4f9d0c81-cdf9-4735-8295-bacbfa3b8a51"
    };
    var instrumentByPositionUid = await client.Instruments.ShareBy(requestByPositionUid, CancellationToken.None);
    ```
