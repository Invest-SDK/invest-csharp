namespace Tinkoff.InvestApi;

public class InvestApiSettings
{
    /**
    * <a href="https://developer.tbank.ru/invest/intro/intro/token">Токен</a>
    *  доступа к API Т-Инвестиций
    */
    public string? AccessToken { get; set; }

    /**
    * Имя Вашего приложения
    */
    public string? AppName { get; set; }

    /**
    * Настройки песочницы
    */
    public bool Sandbox { get; set; }

    /**
     * Таймаут соединения
     */
    public int? Timeout { get; set; }
}