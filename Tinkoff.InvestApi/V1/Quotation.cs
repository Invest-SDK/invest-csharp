namespace Tinkoff.InvestApi.V1;

public partial class Quotation
{
    private const decimal NanoFactor = 1_000_000_000;

    public static implicit operator decimal(Quotation value)
    {
        return value.Units + value.Nano / NanoFactor;
    }

    public static implicit operator Quotation(decimal value)
    {
        var wholePart = (long)value;

        return new Quotation
        {
            Units = wholePart,
            Nano = (int)((value - wholePart) * NanoFactor)
        };
    }

    /// <summary>
    /// Конвертирует цену из валюты в пункты.
    /// <p>Например:</p><pre>{
    /// price = 12.34
    /// min_price_increment = 1
    /// min_price_increment_amount = 0.01
    /// result ---> 1234
    /// }</pre>
    /// </summary>
    /// <param name="minPriceIncrement">Минимальный шаг цены</param>
    /// <param name="minPriceIncrementAmount">Стоимость шага цены в валюте</param>
    /// <returns>Значение цены в пунктах</returns>
    public Quotation ToPt(Quotation minPriceIncrement, Quotation minPriceIncrementAmount)
    {
        var result = this * (decimal) minPriceIncrement / (decimal) minPriceIncrementAmount;
        var wholePart = (long)result;

        return new Quotation
        {
            Units = wholePart,
            Nano = (int)((result - wholePart) * NanoFactor)
        };
    }

    /// <summary>
    /// Конвертирует цену из валюты в пункты.
    /// <p>Например:</p><pre>{
    /// price = 12.34
    /// min_price_increment = 1
    /// min_price_increment_amount = 0.01
    /// result ---> 1234
    /// }</pre>
    /// </summary>
    /// <param name="futureResponse">Данные по фьючерсу</param>
    /// <returns>Значение цены в пунктах</returns>
    public Quotation ToPt(FutureResponse futureResponse)
    {
        return ToPt(futureResponse.Instrument.MinPriceIncrement, futureResponse.Instrument.MinPriceIncrementAmount);
    }

    /// <summary>
    /// Конвертирует цену из пунктов в валюту.
    /// <p>Например:</p><pre>{@code
    /// price = 1234
    /// min_price_increment = 1
    /// min_price_increment_amount = 0.01
    /// result ---> 12.34
    /// }</pre>
    /// </summary>
    /// <param name="minPriceIncrement">Минимальный шаг цены</param>
    /// <param name="minPriceIncrementAmount">Стоимость шага цены в валюте</param>
    /// <returns>Значение цены в валюте</returns>
    public Quotation ToCurrency(Quotation minPriceIncrement, Quotation minPriceIncrementAmount)
    {
        var result = this * (decimal) minPriceIncrementAmount / (decimal) minPriceIncrement;
        var wholePart = (long)result;

        return new Quotation
        {
            Units = wholePart,
            Nano = (int)((result - wholePart) * NanoFactor)
        };
    }

    /// <summary>
    /// Конвертирует цену из пунктов в валюту.
    /// <p>Например:</p><pre>{@code
    /// price = 1234
    /// min_price_increment = 1
    /// min_price_increment_amount = 0.01
    /// result ---> 12.34
    /// }</pre>
    /// </summary>
    /// <param name="futureResponse">Данные по фьючерсу</param>
    /// <returns>Значение цены в валюте</returns>
    public Quotation ToCurrency(FutureResponse futureResponse)
    {
        return ToCurrency(futureResponse.Instrument.MinPriceIncrement, futureResponse.Instrument.MinPriceIncrementAmount);
    }
}