using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Tests.V1;

public class QuotationTest
{
    [Theory]
    [ClassData(typeof(QuotationTestData))]
    public void ConversionTests(Quotation quotation, decimal expectation)
    {
        ((decimal) quotation).Should().Be(expectation);

        quotation = expectation;
        ((decimal) quotation).Should().Be(expectation);
    }

    [Fact]
    public void ConversionPtTests()
    {
        var quotation = 12345.6789M;
        var value = (Quotation) quotation;
        var minPriceIncrement = 0.85M;
        var minPriceIncrementAmount = 0.1M;
        var actual = value.ToPt(minPriceIncrement, minPriceIncrementAmount);
        var expected = (Quotation) 104938.27065M;
        actual.Should().Be(expected);

        var futures = new FutureResponse
        {
            Instrument = new Future
            {
                MinPriceIncrement = minPriceIncrement,
                MinPriceIncrementAmount = minPriceIncrementAmount
            }
        };

        var actual2 = value.ToPt(futures);
        actual2.Should().Be(expected);
    }

    [Fact]
    public void ConversionCurrencyTests()
    {
        var quotation = 12345.6789M;
        var value = (Quotation) quotation;
        var minPriceIncrement = 0.85M;
        var minPriceIncrementAmount = 0.1M;
        var actual = value.ToCurrency(minPriceIncrement, minPriceIncrementAmount);
        var expected = (Quotation) 1452.4328117647M;
        actual.Should().Be(expected);

        var futures = new FutureResponse
        {
            Instrument = new Future
            {
                MinPriceIncrement = minPriceIncrement,
                MinPriceIncrementAmount = minPriceIncrementAmount
            }
        };

        var actual2 = value.ToCurrency(futures);
        actual2.Should().Be(expected);
    }
}