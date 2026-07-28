using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi.V1.Cache;

namespace Tinkoff.InvestApi.Tests.V1.Cache;

public class InstrumentCacheServiceTest
{
    private static InstrumentRequest CreateRequest(InstrumentIdType idType, string id, string classCode = "")
    {
        return new InstrumentRequest
        {
            IdType = idType,
            Id = id,
            ClassCode = classCode
        };
    }

    private static Share CreateTestShare()
    {
        return new Share
        {
            Figi = "BBG004730RP0",
            Ticker = "GAZP",
            ClassCode = "TQBR",
            Uid = "962e2a95-02a9-4171-abd7-aa198dbe643a",
            PositionUid = "4f9d0c81-cdf9-4735-8295-bacbfa3b8a51"
        };
    }

    /// <summary>
    /// Проверяет, что при отключённом кэшировании метод FindBy вызывает fetchFromApi напрямую.
    /// Ожидается, что кэш не используется, результат возвращается из API.
    /// </summary>
    [Fact]
    public async Task FindBy_WhenCachingDisabled_CallsFetchFromApiDirectly()
    {
        // Arrange
        var cacheOptions = Options.Create(new CacheOptions { Enabled = false });
        var instrumentCacheMock = new Mock<IInstrumentCache>();
        var service = new InstrumentCacheService(cacheOptions, instrumentCacheMock.Object);

        var expectedShare = CreateTestShare();
        var fetchCalled = false;
        Func<Task<Share>> fetchFromApi = () =>
        {
            fetchCalled = true;
            return Task.FromResult(expectedShare);
        };

        var request = CreateRequest(InstrumentIdType.Uid, "test-uid");

        // Act
        var result = await service.FindBy(request, fetchFromApi);

        // Assert
        fetchCalled.Should().BeTrue();
        result.Should().BeSameAs(expectedShare);
    }

    /// <summary>
    /// Проверяет, что при отключённом кэшировании метод FindBy не обращается к инструменту кэша.
    /// Ожидается, что instrumentCache.GetOrCreate не вызывается.
    /// </summary>
    [Fact]
    public async Task FindBy_WhenCachingDisabled_DoesNotUseCache()
    {
        // Arrange
        var cacheOptions = Options.Create(new CacheOptions { Enabled = false });
        var instrumentCacheMock = new Mock<IInstrumentCache>();
        var service = new InstrumentCacheService(cacheOptions, instrumentCacheMock.Object);

        Func<Task<Share>> fetchFromApi = () => Task.FromResult(CreateTestShare());
        var request = CreateRequest(InstrumentIdType.Uid, "test-uid");

        // Act
        await service.FindBy(request, fetchFromApi);

        // Assert
        instrumentCacheMock.Verify(
            x => x.GetOrCreate(It.IsAny<string>(), It.IsAny<Func<Task<Share>>>()),
            Times.Never);
    }

    /// <summary>
    /// Проверяет, что при включённом кэшировании метод FindBy использует instrumentCache.GetOrCreate.
    /// Ожидается, что кэш запрашивается с правильным ключом.
    /// </summary>
    [Fact]
    public async Task FindBy_WhenCachingEnabled_UsesInstrumentCache()
    {
        // Arrange
        var cacheOptions = Options.Create(new CacheOptions { Enabled = true });
        var instrumentCacheMock = new Mock<IInstrumentCache>();
        var service = new InstrumentCacheService(cacheOptions, instrumentCacheMock.Object);

        var expectedKey = "share:uid:test-uid";
        var expectedShare = CreateTestShare();
        instrumentCacheMock
            .Setup(x => x.GetOrCreate(It.IsAny<string>(), It.IsAny<Func<Task<Share>>>()))
            .ReturnsAsync(expectedShare);

        var request = CreateRequest(InstrumentIdType.Uid, "test-uid");
        Func<Task<Share>> fetchFromApi = () => Task.FromResult(CreateTestShare());

        // Act
        await service.FindBy(request, fetchFromApi);

        // Assert
        instrumentCacheMock.Verify(
            x => x.GetOrCreate(It.Is<string>(x => x == expectedKey), It.IsAny<Func<Task<Share>>>()),
            Times.Once);
    }

    /// <summary>
    /// Проверяет, что при включённом кэшировании метод FindBy возвращает значение из кэша.
    /// Ожидается, что результат совпадает с закэшированным значением.
    /// </summary>
    [Fact]
    public async Task FindBy_WhenCachingEnabled_ReturnsCachedResult()
    {
        // Arrange
        var cacheOptions = Options.Create(new CacheOptions { Enabled = true });
        var instrumentCacheMock = new Mock<IInstrumentCache>();
        var service = new InstrumentCacheService(cacheOptions, instrumentCacheMock.Object);

        var expectedShare = CreateTestShare();
        instrumentCacheMock
            .Setup(x => x.GetOrCreate(It.IsAny<string>(), It.IsAny<Func<Task<Share>>>()))
            .ReturnsAsync(expectedShare);

        var request = CreateRequest(InstrumentIdType.Uid, "test-uid");
        Func<Task<Share>> fetchFromApi = () => Task.FromResult(new Share());

        // Act
        var result = await service.FindBy(request, fetchFromApi);

        // Assert
        result.Should().BeSameAs(expectedShare);
    }

    /// <summary>
    /// Проверяет, что при промахе кэша (cache miss) метод FindBy вызывает fetchFromApi для получения данных.
    /// Ожидается, что fetchFromApi вызывается один раз, результат сохраняется в кэш.
    /// </summary>
    [Fact]
    public async Task FindBy_WhenCachingEnabled_CallsFetchFromApi_WhenCacheMiss()
    {
        // Arrange
        var cacheOptions = Options.Create(new CacheOptions { Enabled = true });
        var instrumentCacheMock = new Mock<IInstrumentCache>();
        var service = new InstrumentCacheService(cacheOptions, instrumentCacheMock.Object);

        var expectedShare = CreateTestShare();
        var fetchCalled = false;
        Func<Task<Share>> fetchFromApi = () =>
        {
            fetchCalled = true;
            return Task.FromResult(expectedShare);
        };

        instrumentCacheMock
            .Setup(x => x.GetOrCreate(It.IsAny<string>(), It.IsAny<Func<Task<Share>>>()))
            .Returns((string key, Func<Task<Share?>> factory) => factory());

        var request = CreateRequest(InstrumentIdType.Uid, "test-uid");

        // Act
        await service.FindBy(request, fetchFromApi);

        // Assert
        fetchCalled.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что при попадании в кэш (cache hit) метод FindBy не вызывает fetchFromApi повторно.
    /// Ожидается, что fetchFromApi не вызывается, возвращается закэшированное значение.
    /// </summary>
    [Fact]
    public async Task FindBy_WhenCachingEnabled_DoesNotCallFetchFromApi_WhenCacheHit()
    {
        // Arrange
        var cacheOptions = Options.Create(new CacheOptions { Enabled = true });
        var instrumentCacheMock = new Mock<IInstrumentCache>();
        var service = new InstrumentCacheService(cacheOptions, instrumentCacheMock.Object);

        var expectedShare = CreateTestShare();
        var fetchCalled = false;
        Func<Task<Share>> fetchFromApi = () =>
        {
            fetchCalled = true;
            return Task.FromResult(expectedShare);
        };

        // Кэш сразу возвращает значение, имитируя cache hit
        instrumentCacheMock
            .Setup(x => x.GetOrCreate(It.IsAny<string>(), It.IsAny<Func<Task<Share>>>()))
            .ReturnsAsync(expectedShare);

        var request = CreateRequest(InstrumentIdType.Uid, "test-uid");

        // Act
        await service.FindBy(request, fetchFromApi);

        // Assert
        fetchCalled.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что метод FindBy генерирует корректный ключ кэша через InstrumentCacheKey.GetKey для типа инструмента.
    /// Ожидается, что ключ соответствует ожидаемому формату для данного типа инструмента и запроса.
    /// </summary>
    [Fact]
    public async Task FindBy_GeneratesCorrectCacheKey_ForInstrumentType()
    {
        // Arrange
        var cacheOptions = Options.Create(new CacheOptions { Enabled = true });
        var instrumentCacheMock = new Mock<IInstrumentCache>();
        var service = new InstrumentCacheService(cacheOptions, instrumentCacheMock.Object);

        var expectedShare = CreateTestShare();
        string? capturedKey = null;
        instrumentCacheMock
            .Setup(x => x.GetOrCreate(It.IsAny<string>(), It.IsAny<Func<Task<Share>>>()))
            .Returns((string key, Func<Task<Share>> _) =>
            {
                capturedKey = key;
                return Task.FromResult((Share?)expectedShare);
            });

        var request = CreateRequest(InstrumentIdType.Figi, "BBG004730RP0");
        Func<Task<Share>> fetchFromApi = () => Task.FromResult(expectedShare);

        // Act
        await service.FindBy(request, fetchFromApi);

        // Assert
        capturedKey.Should().Be("share:figi:BBG004730RP0");
    }

    /// <summary>
    /// Проверяет, что исключение из fetchFromApi пробрасывается наружу при отключённом кэшировании.
    /// Ожидается, что исключение не перехватывается, вызывающая сторона получает оригинальное исключение.
    /// </summary>
    [Fact]
    public async Task FindBy_PropagatesException_FromFetchFromApi_WhenCachingDisabled()
    {
        // Arrange
        var cacheOptions = Options.Create(new CacheOptions { Enabled = false });
        var instrumentCacheMock = new Mock<IInstrumentCache>();
        var service = new InstrumentCacheService(cacheOptions, instrumentCacheMock.Object);

        var expectedException = new InvalidOperationException("Test exception");
        Func<Task<Share>> fetchFromApi = () => throw expectedException;

        var request = CreateRequest(InstrumentIdType.Uid, "test-uid");

        // Act
        var act = async () => await service.FindBy(request, fetchFromApi);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    /// <summary>
    /// Проверяет, что исключение из instrumentCache.GetOrCreate пробрасывается наружу при включённом кэшировании.
    /// Ожидается, что исключение кэша не перехватывается, вызывающая сторона получает оригинальное исключение.
    /// </summary>
    [Fact]
    public async Task FindBy_PropagatesException_FromInstrumentCache_WhenCachingEnabled()
    {
        // Arrange
        var cacheOptions = Options.Create(new CacheOptions { Enabled = true });
        var instrumentCacheMock = new Mock<IInstrumentCache>();
        var service = new InstrumentCacheService(cacheOptions, instrumentCacheMock.Object);

        var expectedException = new ArgumentException("Cache exception");
        Func<Task<Share>> fetchFromApi = () => Task.FromResult(CreateTestShare());

        instrumentCacheMock
            .Setup(x => x.GetOrCreate(It.IsAny<string>(), It.IsAny<Func<Task<Share>>>()))
            .Throws(expectedException);

        var request = CreateRequest(InstrumentIdType.Uid, "test-uid");

        // Act
        var act = async () => await service.FindBy(request, fetchFromApi);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Cache exception");
    }
}
