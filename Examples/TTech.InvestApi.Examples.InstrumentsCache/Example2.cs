using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace TTech.InvestApi.Examples.InstrumentsCache;

public class Example2(ILogger<Example2> logger, InvestApiClient client)
{
    private const int maxRequests = 1_000;
    private readonly Random rnd = new();

    private readonly IReadOnlyCollection<TestInstrument> instrumentsToRequest =
    [
        new(Uid: "973a3bcc-32f9-488f-8b17-68c03581198d", Figi: "TCS00A109X37", Ticker: "RU000A109X37", ClassCode: "TQCB", Type: TestInstrumentType.BOND),
        new(Uid: "699c3d5a-9ce1-4257-8ed4-b3a8b0c304b1", Figi: "BBG0000K94P9", Ticker: "RU000A0JQRD9", ClassCode: "TQCB", Type: TestInstrumentType.BOND),
        new(Uid: "dc1f4aae-846e-4047-a114-0cf0e8f0732d", Figi: "BBG0036YHM96", Ticker: "RU000A0JSGV0", ClassCode: "TQCB", Type: TestInstrumentType.BOND),
        new(Uid: "c1bcc46b-32c1-47dc-93b4-1db269c103a4", Figi: "BBG0013HSW87", Ticker: "HKDRUB_TOM", ClassCode: "CETS", Type: TestInstrumentType.CURRENCY), // Гонконгский доллар, hkd
        new(Uid: "4587ab1d-a9c9-4910-a0d6-86c7b9c42510", Figi: "BBG0013HRTL0", Ticker: "CNYRUB_TOM", ClassCode: "CETS", Type: TestInstrumentType.CURRENCY), // Китайский юань, cny
        new(Uid: "46a525d0-d33b-4100-a9d3-152045e5fef7", Figi: "BBG005HLSZ23", Ticker: "FXUS", ClassCode: "TQTF", Type: TestInstrumentType.ETF), // FinEx Акции американских компаний
        new(Uid: "39e00aab-1a6d-48e9-8c06-01cbfc8bdc2c", Figi: "BBG000BDNJ29", Ticker: "EWA", ClassCode: "SPBXM", Type: TestInstrumentType.ETF), // iShares MSCI Australia ETF
        new(Uid: "b46a23de-96ba-4d16-b2cc-7567a41306c5", Figi: "BBG00M8C8Y03", Ticker: "FXKZ", ClassCode: "TQTF", Type: TestInstrumentType.ETF), // FinEx Акции казахстанских компаний
        new(Uid: "3da86f70-25a7-4092-bc1b-05cf84dad9fe", Figi: "FKZT09260000", Ticker: "KZU6", ClassCode: "SPBFUT", Type: TestInstrumentType.FUTURE), // KZT-9.26 Курс Казахстанский Тенге - Рубль
        new(Uid: "c74d40a7-1cda-4a9e-9ec1-9aa8fe7b0025", Figi: "FUTWHEAT0826", Ticker: "W4Q6", ClassCode: "SPBFUT", Type: TestInstrumentType.FUTURE), // WHEAT-8.26 Индекс Пшеницы
        new(Uid: "88bc3a7a-625d-4a92-9408-9ac17b10dc4d", Figi: "FSVCB1226000", Ticker: "SCZ6", ClassCode: "SPBFUT", Type: TestInstrumentType.FUTURE), // SVCB-12.26 Совкомбанк
        new(Uid: "06ad1e7d-68c4-4271-9f65-92d44362c610", Figi: "", Ticker: "SR125CI9", ClassCode: "SPBOPT", Type: TestInstrumentType.OPTION), // Сбер Банк CALL 125₽ 19.09
        new(Uid: "4be44fb7-45e1-4854-b9ad-e05d442228b6", Figi: "", Ticker: "GZ95CI7", ClassCode: "SPBOPT", Type: TestInstrumentType.OPTION), // Газпром CALL 95₽ 15.09
        new(Uid: "9bf8cfeb-20cd-4dc4-876b-61eb1cf0d65d", Figi: "", Ticker: "ME126CX9", ClassCode: "SPBOPT", Type: TestInstrumentType.OPTION), // Московская Биржа PUT 126₽ 19.12
        new(Uid: "e6123145-9665-43e0-8413-cd61b8aa9b13", Figi: "BBG004730N88", Ticker: "SBER", ClassCode: "TQBR", Type: TestInstrumentType.SHARE),
        new(Uid: "cbdf1d32-5758-490e-a2b1-780eaa79bdf7", Figi: "TCS10A0JNAB6", Ticker: "ABIO", ClassCode: "TQBR", Type: TestInstrumentType.SHARE),
        new(Uid: "926fdfbf-4b07-47c9-8928-f49858ca33f2", Figi: "BBG002W2FT69", Ticker: "ABRD", ClassCode: "TQBR", Type: TestInstrumentType.SHARE),
    ];

    public async Task Run(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting...");
        var tasks = new List<Task<object>>();
        for (int i = 0; i < maxRequests; i++)
        {
            await Task.Delay(rnd.Next(500), cancellationToken);
            // Make large number of random requests to get instrument info.
            // Only MAX(instrumentsToRequest.Count()) requests will be sent to InvestApi.
            cancellationToken.ThrowIfCancellationRequested();
            async Task<object> task()
            {
                var instrumentToRequest = instrumentsToRequest.ElementAt(rnd.Next(instrumentsToRequest.Count));
                var allowedIdTypes = GetAllowedInstrumentRequestIdTypes(instrumentToRequest.Type);
                var idType = allowedIdTypes[rnd.Next(allowedIdTypes.Length)];
                (string id, string classCode) = idType switch
                {
                    InstrumentIdType.Uid => (instrumentToRequest.Uid, string.Empty),
                    InstrumentIdType.Figi => (instrumentToRequest.Figi, string.Empty),
                    InstrumentIdType.Ticker => (instrumentToRequest.Ticker, instrumentToRequest.ClassCode),
                    _ => throw new ArgumentOutOfRangeException()
                };
                var request = new InstrumentRequest
                {
                    IdType = idType,
                    ClassCode = classCode,
                    Id = id
                };
                return await InstrumentBy(request, instrumentToRequest.Type, cancellationToken);
            }
            tasks.Add(task());
        }
        await Task.WhenAll(tasks);
        logger.LogInformation("Done");
    }

    private static InstrumentIdType[] GetAllowedInstrumentRequestIdTypes(TestInstrumentType instrumentType)
    {
        return instrumentType switch
        {
            TestInstrumentType.BOND => [InstrumentIdType.Uid, InstrumentIdType.Figi, InstrumentIdType.Ticker],
            TestInstrumentType.CURRENCY => [InstrumentIdType.Uid, InstrumentIdType.Figi, InstrumentIdType.Ticker],
            TestInstrumentType.ETF => [InstrumentIdType.Uid, InstrumentIdType.Figi, InstrumentIdType.Ticker],
            TestInstrumentType.FUTURE => [InstrumentIdType.Uid, InstrumentIdType.Figi, InstrumentIdType.Ticker],
            TestInstrumentType.OPTION => [InstrumentIdType.Uid, InstrumentIdType.Ticker],
            TestInstrumentType.SHARE => [InstrumentIdType.Uid, InstrumentIdType.Figi, InstrumentIdType.Ticker],
            TestInstrumentType.STRUCTURED_NOTE => [InstrumentIdType.Uid, InstrumentIdType.Figi, InstrumentIdType.Ticker],
            _ => throw new ArgumentOutOfRangeException(nameof(instrumentType), instrumentType, null)
        };
    }

    private async Task<object> InstrumentBy(InstrumentRequest request, TestInstrumentType instrumentType, CancellationToken cancellationToken)
    {
        return instrumentType switch
        {
            TestInstrumentType.BOND => await client.Instruments.BondBy(request, cancellationToken),
            TestInstrumentType.CURRENCY => await client.Instruments.CurrencyBy(request, cancellationToken),
            TestInstrumentType.ETF => await client.Instruments.EtfBy(request, cancellationToken),
            TestInstrumentType.FUTURE => await client.Instruments.FutureBy(request, cancellationToken),
            TestInstrumentType.OPTION => await client.Instruments.OptionBy(request, cancellationToken),
            TestInstrumentType.SHARE => await client.Instruments.ShareBy(request, cancellationToken),
            TestInstrumentType.STRUCTURED_NOTE => await client.Instruments.StructuredNoteBy(request, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(instrumentType), instrumentType, null)
        };
    }
}
