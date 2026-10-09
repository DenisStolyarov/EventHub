using EventHub.Events.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace EventHub.Events.UnitTests.Services;

public sealed class RedisCacheServiceTests
{
    private const string CacheKey = "event:1";

    private readonly Mock<IDatabase> _databaseMock;
    private readonly RedisCacheService _cacheService;

    public RedisCacheServiceTests()
    {
        Mock<IConnectionMultiplexer> multiplexerMock = new();
        _databaseMock = new Mock<IDatabase>();

        multiplexerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _cacheService = new RedisCacheService(multiplexerMock.Object, NullLogger<RedisCacheService>.Instance);
    }

    [Fact]
    public async Task GetAsync_RedisThrows_ReturnsDefaultInsteadOfPropagating()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis is unavailable"));

        // Act
        CacheTestValue? result = await _cacheService.GetAsync<CacheTestValue>(CacheKey, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_CorruptedJson_ReturnsDefaultInsteadOfPropagating()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)"{ corrupted }");

        // Act
        CacheTestValue? result = await _cacheService.GetAsync<CacheTestValue>(CacheKey, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ValidJson_ReturnsDeserializedValue()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.StringGetAsync(CacheKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)"{\"Name\":\"cached\"}");

        // Act
        CacheTestValue? result = await _cacheService.GetAsync<CacheTestValue>(CacheKey, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("cached");
    }

    [Fact]
    public async Task SetAsync_RedisThrows_DoesNotPropagate()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis is unavailable"));

        // Act
        Func<Task> act = () => _cacheService.SetAsync(CacheKey, new CacheTestValue("value"), TimeSpan.FromMinutes(10), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_RedisThrows_DoesNotPropagate()
    {
        // Arrange
        _databaseMock
            .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis is unavailable"));

        // Act
        Func<Task> act = () => _cacheService.RemoveAsync(CacheKey, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    private sealed record CacheTestValue(string Name);
}
