using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Felis.Client;

internal class CacheService
{
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;
    private readonly IMemoryCache _cache;

    public CacheService(IMemoryCache cache,
        IConfiguration configuration)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));


        var slidingExpiration = int.Parse(configuration["Cache:SlidingExpiration"] ??
                                          "3600");

        var absoluteExpiration = int.Parse(configuration["Cache:AbsoluteExpiration"] ??
                                           "3600");

        var maxSizeBytes = int.Parse(configuration["Cache:MaxSizeBytes"] ??
                                     "1000");

        _cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(slidingExpiration))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(absoluteExpiration))
            .SetSize(maxSizeBytes)
            .SetPriority(CacheItemPriority.High);
    }

    public void Set(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        _cache.Remove(key);
        _cache.Set(key, value, _cacheEntryOptions);
    }

    public T? Get<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        var found = _cache.TryGetValue(key, out T? result);

        return !found ? default : result;
    }
}