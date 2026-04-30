using System;

namespace iLearn.Application.Common
{
    public static class AssignmentStatusKeys
    {
        public const int DueSoonWindowDays = 7;

        public static class Batch
        {
            public const string Completed = "Completed";
            public const string Upcoming = "Upcoming";
            public const string Expired = "Expired";
            public const string InProgress = "InProgress";
        }

        public static class Learner
        {
            public const string Completed = "Completed";
            public const string Upcoming = "Upcoming";
            public const string Overdue = "Overdue";
            public const string InProgress = "InProgress";
            public const string NotStarted = "NotStarted";
        }

        public static string GetBatchStatus(bool hasEnrollments, bool allCompleted, DateTime? startDate, DateTime? dueDate, DateTime currentDate)
        {
            if (hasEnrollments && allCompleted)
            {
                return Batch.Completed;
            }

            if (startDate.HasValue && startDate.Value > currentDate)
            {
                return Batch.Upcoming;
            }

            if (dueDate.HasValue && dueDate.Value < currentDate)
            {
                return Batch.Expired;
            }

            return Batch.InProgress;
        }

        public static string GetLearnerStatus(bool isCompleted, double progress)
        {
            if (isCompleted)
            {
                return Learner.Completed;
            }

            return progress > 0
                ? Learner.InProgress
                : Learner.NotStarted;
        }

        public static string GetScheduledLearnerStatus(bool isCompleted, double progress, DateTime? startDate, DateTime? dueDate, DateTime currentDate)
        {
            if (isCompleted)
            {
                return Learner.Completed;
            }

            if (startDate.HasValue && startDate.Value > currentDate)
            {
                return Learner.Upcoming;
            }

            if (dueDate.HasValue && dueDate.Value < currentDate)
            {
                return Learner.Overdue;
            }

            return progress > 0
                ? Learner.InProgress
                : Learner.NotStarted;
        }

        public static DateTime GetDueSoonCutoff(DateTime currentDate)
        {
            return currentDate.Date.AddDays(DueSoonWindowDays);
        }

        public static bool IsDueSoon(bool isCompleted, DateTime? dueDate, DateTime currentDate)
        {
            if (isCompleted || !dueDate.HasValue)
            {
                return false;
            }

            var today = currentDate.Date;
            var dueDateValue = dueDate.Value.Date;
            return dueDateValue >= today && dueDateValue <= GetDueSoonCutoff(today);
        }
    }
}