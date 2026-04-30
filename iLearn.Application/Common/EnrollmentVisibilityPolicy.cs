using System;

namespace iLearn.Application.Common
{
    public static class EnrollmentVisibilityPolicy
    {
        public const int CompletedHistoryRetentionMonths = 1;

        public static DateTime GetCompletedHistoryCutoff(DateTime currentDate)
        {
            return currentDate.AddMonths(-CompletedHistoryRetentionMonths);
        }

        public static bool ShouldShowCompletedEnrollment(DateTime? completedDate, DateTime currentDate)
        {
            return completedDate.HasValue && completedDate.Value >= GetCompletedHistoryCutoff(currentDate);
        }
    }
}