using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Tests.V1;

public class MoneyValueTest
{
    [Theory]
    [ClassData(typeof(QuotationTestData))]
    public void ConvertToDecimal(Quotation quotation, decimal expectation)
    {
        var moneyValue = new MoneyValue {Units = quotation.Units, Nano = quotation.Nano};
        ((decimal) moneyValue).Should().Be(expectation);
    }

    [Fact]
    public void ConversionPtTests()
    {
        var quotation = 12345.6789M;
        var value = (Quotation) quotation;
        var moneyValue = new MoneyValue {Units = value.Units, Nano = value.Nano};
        var minPriceIncrement = 0.85M;
        var minPriceIncrementAmount = 0.1M;
        var actual = moneyValue.ToPt(minPriceIncrement, minPriceIncrementAmount);
        var expected = (Quotation) 104938.27065M;
        actual.Should().Be(new MoneyValue {Units = expected.Units, Nano = expected.Nano, Currency = "pt."});

        var futures = new FutureResponse
        {
            Instrument = new Future
            {
                MinPriceIncrement = minPriceIncrement,
                MinPriceIncrementAmount = minPriceIncrementAmount,
                Currency = "rub"
            }
        };

        var actual2 = moneyValue.ToPt(futures);
        actual2.Should().Be(new MoneyValue {Units = expected.Units, Nano = expected.Nano, Currency = "pt."});
    }

    [Fact]
    public void ConversionCurrencyTests()
    {
        var value = new MoneyValue{ Units = 12345, Nano = 678900000, Currency = "pt."};
        var minPriceIncrement = 0.85M;
        var minPriceIncrementAmount = 0.1M;
        var actual = value.ToCurrency(minPriceIncrement, minPriceIncrementAmount);
        var expected = (Quotation) 1452.4328117647M;
        actual.Should().Be(new MoneyValue {Units = expected.Units, Nano = expected.Nano});

        var futures = new FutureResponse
        {
            Instrument = new Future
            {
                MinPriceIncrement = minPriceIncrement,
                MinPriceIncrementAmount = minPriceIncrementAmount,
                Currency = "rub"
            }
        };

        var actual2 = value.ToCurrency(futures);
        actual2.Should().Be(new MoneyValue {Units = expected.Units, Nano = expected.Nano, Currency = "rub"});
    }
}