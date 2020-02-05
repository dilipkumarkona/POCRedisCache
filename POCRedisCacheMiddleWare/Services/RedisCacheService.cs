using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace POCRedisCacheMiddleWare
{
    public class RedisCacheService  : IRedisCacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly RedisCacheSettings _redisCacheSettings;
        public RedisCacheService(IDistributedCache distributedCache, RedisCacheSettings redisCacheSettings)
        {
            _distributedCache = distributedCache;
            _redisCacheSettings = redisCacheSettings;
        }
        public RedisCacheSettings RedisCacheSettings => _redisCacheSettings;

        public async Task CacheResponseAsync(string cacheKey, object response, int timeTimeLive = 600)
        {
            if (response == null)
            {
                return;
            }

            var serializedResponse = JsonConvert.SerializeObject(response);

            await _distributedCache.SetStringAsync(cacheKey, serializedResponse, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(timeTimeLive)
            });
        }

        public async Task<string> GetCachedResponseAsync(string cacheKey)
        {
            var cachedResponse = await _distributedCache.GetStringAsync(cacheKey);
            return string.IsNullOrEmpty(cachedResponse) ? null : cachedResponse;
        }
    }
}
