namespace VoyageEnergyAdvisor.Core.Services.CacheService
{
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;

    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public bool TryGetCachedItem<T>(string cacheKey, out T? cachedItem)
        {
            if (_cache.TryGetValue(cacheKey, out cachedItem))
            {
                _logger.LogInformation($"Cache hit for key: {cacheKey}");
                return true;
            }
            else
            {
                _logger.LogInformation($"Cache miss for key: {cacheKey}");
                return false;
            }
        }

        public void CacheItem<T>(string cacheKey, T item, TimeSpan absoluteExpiration, TimeSpan slidingExpiration)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration,
                SlidingExpiration = slidingExpiration
            };
            _cache.Set(cacheKey, item, cacheEntryOptions);
            _logger.LogInformation($"Item cached with key: {cacheKey}");
        }

        public string GenerateCacheKey(params object[] keyParts)
        {
            return string.Join("_", keyParts.Select(k => k?.ToString() ?? string.Empty));
        }

        public void Remove(string cacheKey)
        {
            _cache.Remove(cacheKey);
            _logger.LogInformation($"Item removed from cache with key: {cacheKey}");
        }
    }

}
