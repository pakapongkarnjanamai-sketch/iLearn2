using Microsoft.Extensions.Caching.Memory;

namespace iLearn.API.Services
{
    public static class ResourceStatsCache
    {
        public const string SummaryStatsKey = "resources:summary-stats";
        public const string ServerStatsKey = "resources:server-stats";

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
