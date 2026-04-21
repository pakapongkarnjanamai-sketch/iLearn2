using Microsoft.Extensions.Caching.Memory;

namespace iLearn.Infrastructure.Services
{
    /// <summary>
    /// Resource statistics cache key/options helper. Lives in Infrastructure
    /// because it owns the IMemoryCache contract used by Resource read/refresh
    /// flows. Controllers depend on this through DI; see <c>IResourceStatsCache</c>
    /// for the abstraction.
    /// </summary>
    public static class ResourceStatsCache
    {
        public const string SummaryStatsKey = "resources:summary-stats";
        public const string ServerStatsKey  = "resources:server-stats";

        public static MemoryCacheEntryOptions DefaultOptions { get; } = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        public static void Invalidate(IMemoryCache cache)
        {
            cache.Remove(SummaryStatsKey);
            cache.Remove(ServerStatsKey);
        }
    }
}
