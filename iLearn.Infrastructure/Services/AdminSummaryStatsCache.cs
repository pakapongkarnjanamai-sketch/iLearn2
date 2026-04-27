using Microsoft.Extensions.Caching.Memory;

namespace iLearn.Infrastructure.Services
{
    public static class AdminSummaryStatsCache
    {
        public const string DivisionsSummaryKey = "admin:divisions:summary-stats";
        public const string CourseTypesSummaryKey = "admin:course-types:summary-stats";
        public const string EnrollmentsSummaryKey = "admin:enrollments:summary-stats";
        public const string LearningLogsSummaryKey = "admin:learning-logs:summary-stats";

        public static MemoryCacheEntryOptions SummaryOptions { get; } = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };

        public static void InvalidateDivisions(IMemoryCache cache)
        {
            cache.Remove(DivisionsSummaryKey);
        }

        public static void InvalidateCourseTypes(IMemoryCache cache)
        {
            cache.Remove(CourseTypesSummaryKey);
        }

        public static void InvalidateEnrollments(IMemoryCache cache)
        {
            cache.Remove(EnrollmentsSummaryKey);
        }

        public static void InvalidateLearningLogs(IMemoryCache cache)
        {
            cache.Remove(LearningLogsSummaryKey);
        }

        public static void InvalidateAll(IMemoryCache cache)
        {
            cache.Remove(DivisionsSummaryKey);
            cache.Remove(CourseTypesSummaryKey);
            cache.Remove(EnrollmentsSummaryKey);
            cache.Remove(LearningLogsSummaryKey);
        }
    }
}