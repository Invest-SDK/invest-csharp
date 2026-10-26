# Интеграционные тесты для MarketDataStreamWrapper

## Реализованные тесты

Файл: `Tinkoff.InvestApi.Tests/Marketdata/MarketDataStreamWrapperTests.cs`

### Базовые тесты

1. **Constructor_ShouldCreateWrapper** - Проверяет создание обёртки
2. **Disconnect_ShouldCompleteWithoutError** - Проверяет корректное завершение работы

## Требуемые интеграционные тесты

Для полноценного тестирования необходимы следующие интеграционные тесты с использованием TestServer:

### Кейс 1: Подписка до подключения

```csharp
[Fact]
public async Task SubscribeBeforeConnect_ShouldSendRequestsOnceAfterConnect()
{
    // Пользователь сначала подписался, потом подключился к стриму
    // Ожидается, что запросы ушли после подключения однократно
    
    // Arrange
    var wrapper = CreateWrapper();
    var instrument = new CandleInstrument { InstrumentId = "BBG004730N88" };
    
    // Act - подписываемся ДО подключения
    var subscribeTask = wrapper.SubscribeCandlesAsync(new[] { instrument });
    
    // Подключаемся
    var connectTask = wrapper.ConnectAsync();
    await Task.WhenAll(subscribeTask, connectTask);
    
    // Assert
    MarketDataStreamTestServiceImpl.SubscribeRequestCount.Should().Be(1);
}
```

### Кейс 2: Подписка после подключения с ожиданием ответа

```csharp
[Fact]
public async Task SubscribeAfterConnect_ShouldCompleteAfterServerResponse()
{
    // Пользователь подключился, затем подписался и ждёт ответа от сервера
    // Ожидается, что метод Subscribe закончит выполнение после ответа сервера
    
    // Arrange
    MarketDataStreamTestServiceImpl.ResponseDelayMs = 300;
    var wrapper = CreateWrapper();
    await wrapper.ConnectAsync();
    
    // Act - подписываемся и замеряем время
    var subscribeStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    await wrapper.SubscribeCandlesAsync(new[] { instrument });
    var subscribeDuration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - subscribeStart;
    
    // Assert
    subscribeDuration.Should().BeGreaterThanOrEqualTo(300 - 50);
}
```

### Кейс 3: Переподключение с восстановлением подписок

```csharp
[Fact]
public async Task Reconnect_ShouldResendSubscriptions()
{
    // Пользователь подключился, успешно подписался, затем сервер возвращает ошибку
    // и выполняется переподключение с восстановлением подписок
    // Ожидается, что запрос на подписку будет отправлен повторно
    
    // Arrange
    var wrapper = CreateWrapper();
    await wrapper.ConnectAsync();
    await wrapper.SubscribeCandlesAsync(new[] { instrument });
    
    var initialRequestCount = MarketDataStreamTestServiceImpl.SubscribeRequestCount;
    
    // Эмулируем ошибку сервера
    MarketDataStreamTestServiceImpl.ShouldThrowException = true;
    
    // Ждем переподключения
    await Task.Delay(500);
    
    // Assert
    MarketDataStreamTestServiceImpl.SubscribeRequestCount
        .Should().BeGreaterThan(initialRequestCount);
}
```

## Необходимые компоненты для интеграционных тестов

### 1. TestServer с GrpcTestServerFixture

Использовать существующий `GrpcTestServerFixture` из `ResilenceDuplexStreamWrapperTests`.

### 2. MarketDataStreamTestServiceImpl

Создать тестовый сервис с управляемым поведением:

```csharp
public class MarketDataStreamTestServiceImpl : MarketDataStreamService.MarketDataStreamServiceBase
{
    public static bool ShouldThrowException { get; set; }
    public static bool ShouldSendSubResponse { get; set; }
    public static SubscriptionStatus SubscriptionResponseStatus { get; set; }
    public static int ResponseDelayMs { get; set; }
    public static int ReconnectDelayMs { get; set; }
    
    public static int ConnectionCount { get; }
    public static int SubscribeRequestCount { get; }
    public static IReadOnlyCollection<string> ReceivedSubscriptionIds { get; }
    
    public static void Reset();
    
    public override Task MarketDataStream(...);
}
```

### 3. Proto-файл

Для компиляции требуется `marketdata.proto` с зависимостями:
- `common.proto`
- `google/api/field_behavior.proto`
- `google/protobuf/timestamp.proto`

**Проблема:** marketdata.proto имеет много зависимостей, которые сложно настроить в тестовом проекте.

**Решение:** Создать упрощённый тестовый proto-файл с минимальным набором сообщений, достаточным для тестирования.

## Запуск тестов

```bash
# Запустить все тесты MarketDataStreamWrapper
dotnet test --filter "FullyQualifiedName~MarketDataStreamWrapperTests"

# Запустить с подробным выводом
dotnet test --filter "FullyQualifiedName~MarketDataStreamWrapperTests" --logger "console;verbosity=detailed"
```

## Примечания

1. **HandleConnectionAsync** теперь пропускает первое подключение и отправляет запросы только при переподключении
2. **Subscribe*Async** ждут завершения HandleConnectionAsync перед продолжением
3. **_connectionHandlerLock** обеспечивает синхронизацию между HandleConnectionAsync и Subscribe*Async
4. **_firstConnectionCompleted** отслеживает, было ли уже первое подключение
