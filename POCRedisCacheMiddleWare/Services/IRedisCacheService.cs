using System;
using System.Threading.Tasks;

namespace POCRedisCacheMiddleWare
{
    public interface IRedisCacheService
    {
        RedisCacheSettings RedisCacheSettings { get; }
        Task CacheResponseAsync(string cacheKey, object response, int timeTimeLive);
        Task<string> GetCachedResponseAsync(string cacheKey);
    }
}
