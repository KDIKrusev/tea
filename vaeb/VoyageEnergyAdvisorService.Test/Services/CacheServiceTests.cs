using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using VoyageEnergyAdvisor.Core.Services.CacheService;
using Xunit;

namespace VoyageEnergyAdvisorService.Test.Services;

/// <summary>
/// Unit tests for CacheService.
/// Tests caching operations, expiration, and key generation.
/// </summary>
public class CacheServiceTests
{
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ILogger<CacheService>> _mockLogger;
    private readonly CacheService _cacheService;

    public CacheServiceTests()
    {
        _mockCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger<CacheService>>();
        _cacheService = new CacheService(_mockCache.Object, _mockLogger.Object);
    }

    [Fact]
    public void TryGetCachedItem_ItemExists_ReturnsTrue()
    {
        // Arrange
        var cacheKey = "test_key";
        var expectedValue = "test_value";
        object? cachedValue = expectedValue;

        _mockCache
            .Setup(c => c.TryGetValue(cacheKey, out cachedValue))
            .Returns(true);

        // Act
        var result = _cacheService.TryGetCachedItem<string>(cacheKey, out var retrievedValue);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedValue, retrievedValue);
    }

    [Fact]
    public void TryGetCachedItem_ItemDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var cacheKey = "nonexistent_key";
        object? cachedValue = null;

        _mockCache
            .Setup(c => c.TryGetValue(cacheKey, out cachedValue))
            .Returns(false);

        // Act
        var result = _cacheService.TryGetCachedItem<string>(cacheKey, out var retrievedValue);

        // Assert
        Assert.False(result);
        Assert.Null(retrievedValue);
    }

    [Fact]
    public void TryGetCachedItem_ComplexObject_ReturnsCorrectly()
    {
        // Arrange
        var cacheKey = "complex_key";
        var expectedObject = new TestClass { Id = 1, Name = "Test" };
        object? cachedValue = expectedObject;

        _mockCache
            .Setup(c => c.TryGetValue(cacheKey, out cachedValue))
            .Returns(true);

        // Act
        var result = _cacheService.TryGetCachedItem<TestClass>(cacheKey, out var retrievedValue);

        // Assert
        Assert.True(result);
        Assert.NotNull(retrievedValue);
        Assert.Equal(1, retrievedValue.Id);
        Assert.Equal("Test", retrievedValue.Name);
    }

    [Fact]
    public void CacheItem_ValidItem_CachesSuccessfully()
    {
        // Arrange
        var cacheKey = "test_key";
        var item = "test_value";
        var absoluteExpiration = TimeSpan.FromHours(1);
        var slidingExpiration = TimeSpan.FromMinutes(15);
        
        var mockCacheEntry = new Mock<ICacheEntry>();
        _mockCache
            .Setup(c => c.CreateEntry(cacheKey))
            .Returns(mockCacheEntry.Object);

        // Act
        _cacheService.CacheItem(cacheKey, item, absoluteExpiration, slidingExpiration);

        // Assert
        _mockCache.Verify(c => c.CreateEntry(cacheKey), Times.Once);
    }

    [Fact]
    public void CacheItem_ComplexObject_CachesSuccessfully()
    {
        // Arrange
        var cacheKey = "complex_key";
        var item = new TestClass { Id = 42, Name = "Complex" };
        var absoluteExpiration = TimeSpan.FromDays(1);
        var slidingExpiration = TimeSpan.FromHours(2);
        
        var mockCacheEntry = new Mock<ICacheEntry>();
        _mockCache
            .Setup(c => c.CreateEntry(cacheKey))
            .Returns(mockCacheEntry.Object);

        // Act
        _cacheService.CacheItem(cacheKey, item, absoluteExpiration, slidingExpiration);

        // Assert
        _mockCache.Verify(c => c.CreateEntry(cacheKey), Times.Once);
    }

    [Fact]
    public void GenerateCacheKey_SinglePart_ReturnsKey()
    {
        // Arrange
        var part1 = "user";

        // Act
        var result = _cacheService.GenerateCacheKey(part1);

        // Assert
        Assert.Equal("user", result);
    }

    [Fact]
    public void GenerateCacheKey_MultipleParts_ReturnsCombinedKey()
    {
        // Arrange
        var part1 = "user";
        var part2 = "profile";
        var part3 = 123;

        // Act
        var result = _cacheService.GenerateCacheKey(part1, part2, part3);

        // Assert
        Assert.Equal("user_profile_123", result);
    }

    [Fact]
    public void GenerateCacheKey_WithNullPart_HandlesGracefully()
    {
        // Arrange
        var part1 = "user";
        object? part2 = null;
        var part3 = "data";

        // Act
        var result = _cacheService.GenerateCacheKey(part1, part2!, part3);

        // Assert
        Assert.Equal("user__data", result);
    }

    [Fact]
    public void GenerateCacheKey_EmptyArray_ReturnsEmptyString()
    {
        // Act
        var result = _cacheService.GenerateCacheKey();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Remove_ExistingKey_RemovesFromCache()
    {
        // Arrange
        var cacheKey = "key_to_remove";

        // Act
        _cacheService.Remove(cacheKey);

        // Assert
        _mockCache.Verify(c => c.Remove(cacheKey), Times.Once);
    }

    [Fact]
    public void Remove_NonExistentKey_CallsRemove()
    {
        // Arrange
        var cacheKey = "nonexistent_key";

        // Act
        _cacheService.Remove(cacheKey);

        // Assert
        // Should still call Remove even if key doesn't exist
        _mockCache.Verify(c => c.Remove(cacheKey), Times.Once);
    }

    private class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
