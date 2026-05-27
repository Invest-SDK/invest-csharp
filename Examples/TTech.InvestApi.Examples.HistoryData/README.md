#### О примере

Пример приложения для скачивания [истории котировок](https://developer.tbank.ru/invest/services/history-md/head-history-md) и сохранения значений в базе данных.

#### Принцип работы

1. Обращение к [API](https://developer.tbank.ru/invest/api) для запроса архива `*.csv.gz`
    ```
    https://invest-public-api.tinkoff.ru/history-trades/<date>?instrumentId=<id>
    ```
2. Распаковка архива и парсинг записей CSV
3. Сохранение записей в таблицу базы данных PostgreSQL
4. Сохранение исходного архива в объектное хранилище S3

#### Как запустить

1. Перейти в папку с проектом
2. Запустить экземпляры PostgreSQL и S3 в Docker
    ```
    docker compose -f docker-compose.yml up -d
    ```
    В ходе запуска сервисов будут применены миграции *liquibase* для создания базы и необходимых таблиц, а также создан бакет в объектном хранилище.
3. В основном файле `Program.cs` указать требуемые значения переменных:

    - `date` - дата котировок,
    - `instrumentIds` - перечень идентификаторов инструментов.

    Например:
    ```csharp
    var date = new DateOnly(2023, 9, 27);
    var instrumentIds = new[] { "MASI_SPBXM", "ONTO_SPBXM", "HYDR_SMAL", "STAG_SPBXM" };
    ```
4. Выполнить сборку и запуск проекта
    ```
    dotnet build
    dotnet run
    ```
5. После завершения работы приложения загруженные данные можно увидеть в таблице `t_history_trade`. Таблица `t_archive` хранит перечень сохраненных архивов для предотвращения повторной загрузки. Строка подключения выглядит следующим образом:
    ```
    Host=localhost;Username=postgres;Password=postgres;Database=history_data
    ```
6. В консоли управления объектным хранилищем `http://localhost:9001` в бакете `history-data` можно посмотреть загруженные файлы. Логин и пароль для доступа
    ```
    minioadmin / minioadmin
    ```