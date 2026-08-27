namespace VoyageEnergyAdvisor.Core.Services.CacheService
{
    public interface ICacheService
    {
        bool TryGetCachedItem<T>(string cacheKey, out T? cachedItem);
        void CacheItem<T>(string cacheKey, T item, TimeSpan absoluteExpiration, TimeSpan slidingExpiration);
        string GenerateCacheKey(params object[] keyParts);
        void Remove(string cacheKey);
    }
}
