using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Site.DesignTokens;

public sealed class DesignTokenCssCache : IDesignTokenCssCache
{
    private const string CacheKeyPrefix = "site.design-tokens.css.";
    private readonly ConcurrentDictionary<string, byte> _cacheKeys = new(StringComparer.Ordinal);
    private readonly IMemoryCache _memoryCache;
    private readonly IDesignTokenCssGenerator _generator;

    public DesignTokenCssCache(IMemoryCache memoryCache, IDesignTokenCssGenerator generator)
    {
        _memoryCache = memoryCache;
        _generator = generator;
    }

    public string GetCss(Guid tenantKey)
    {
        var cacheKey = CacheKeyPrefix + tenantKey.ToString("N");
        _cacheKeys.TryAdd(cacheKey, 0);

        return _memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return _generator.GenerateCss(tenantKey);
        }) ?? string.Empty;
    }

    public void Invalidate()
    {
        foreach (var cacheKey in _cacheKeys.Keys)
        {
            _memoryCache.Remove(cacheKey);
            _cacheKeys.TryRemove(cacheKey, out _);
        }
    }
}
