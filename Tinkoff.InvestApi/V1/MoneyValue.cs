namespace Tinkoff.InvestApi.V1;

public partial class MoneyValue
{
    private const decimal NanoFactor = 1_000_000_000;

    public static implicit operator decimal(MoneyValue value)
    {
        return value.Units + value.Nano / NanoFactor;
    }

    public string Format()
    {
        return $"{(decimal) this} {Currency}";
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
    public MoneyValue ToPt(Quotation minPriceIncrement, Quotation minPriceIncrementAmount)
    {
        if (Currency == null || !Currency.Equals("pt."))
        {
            var result = this * (decimal) minPriceIncrement / (decimal) minPriceIncrementAmount;
            var wholePart = (long)result;

            return new MoneyValue{
                Units = wholePart,
                Nano = (int)((result - wholePart) * NanoFactor),
                Currency = "pt."
            };
        }
        return this;
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
    public MoneyValue ToPt(FutureResponse futureResponse)
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
    /// <param name="currency">Валюта после конвертации</param>
    /// <returns>Значение цены в валюте</returns>
    public MoneyValue ToCurrency(Quotation minPriceIncrement, Quotation minPriceIncrementAmount, string? currency = null)
    {
        if (Currency == null || Currency.Equals("pt."))
        {
            var value = (decimal) this;
            var result = value * (decimal) minPriceIncrementAmount / (decimal) minPriceIncrement;
            var wholePart = (long)result;

            if (currency != null)
            {
                return new MoneyValue
                {
                    Units = wholePart,
                    Nano = (int)((result - wholePart) * NanoFactor),
                    Currency = currency
                };
            }
            return new MoneyValue
            {
                Units = wholePart,
                Nano = (int)((result - wholePart) * NanoFactor)
            };
        }
        return this;
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
    public MoneyValue ToCurrency(FutureResponse futureResponse)
    {
        return ToCurrency(futureResponse.Instrument.MinPriceIncrement, futureResponse.Instrument.MinPriceIncrementAmount, futureResponse.Instrument.Currency);
    }
}