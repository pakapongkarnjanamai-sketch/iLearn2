using iLearn.Application.Common;

namespace iLearn.Tests
{
    public class AssignmentStatusKeysTests
    {
        [Theory]
        [InlineData(true, 0, AssignmentStatusKeys.Learner.Completed)]
        [InlineData(true, 100, AssignmentStatusKeys.Learner.Completed)]
        [InlineData(false, 25, AssignmentStatusKeys.Learner.InProgress)]
        [InlineData(false, 0, AssignmentStatusKeys.Learner.NotStarted)]
        public void GetLearnerStatus_ReturnsCanonicalKeys(bool isCompleted, double progress, string expected)
        {
            var result = AssignmentStatusKeys.GetLearnerStatus(isCompleted, progress);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(true, 0, null, null, AssignmentStatusKeys.Learner.Completed)]
        [InlineData(false, 0, 1, null, AssignmentStatusKeys.Learner.Upcoming)]
        [InlineData(false, 0, null, -1, AssignmentStatusKeys.Learner.Overdue)]
        [InlineData(false, 35, null, -1, AssignmentStatusKeys.Learner.Overdue)]
        [InlineData(false, 35, null, null, AssignmentStatusKeys.Learner.InProgress)]
        [InlineData(false, 0, null, null, AssignmentStatusKeys.Learner.NotStarted)]
        public void GetScheduledLearnerStatus_ReturnsCanonicalKeys(bool isCompleted, double progress, int? startOffsetDays, int? dueOffsetDays, string expected)
        {
            var now = new DateTime(2026, 4, 30, 12, 0, 0, DateTimeKind.Utc);
            DateTime? startDate = startOffsetDays.HasValue ? now.AddDays(startOffsetDays.Value) : null;
            DateTime? dueDate = dueOffsetDays.HasValue ? now.AddDays(dueOffsetDays.Value) : null;

            var result = AssignmentStatusKeys.GetScheduledLearnerStatus(isCompleted, progress, startDate, dueDate, now);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void DueSoonWindow_UsesSevenDaySharedThreshold()
        {
            Assert.Equal(7, AssignmentStatusKeys.DueSoonWindowDays);
        }

        [Theory]
        [InlineData(false, 0, true)]
        [InlineData(false, 7, true)]
        [InlineData(false, 8, false)]
        [InlineData(true, 3, false)]
        [InlineData(false, null, false)]
        public void IsDueSoon_UsesSharedThresholdAndExcludesCompleted(bool isCompleted, int? dueOffsetDays, bool expected)
        {
            var today = new DateTime(2026, 4, 30, 12, 0, 0, DateTimeKind.Utc);
            DateTime? dueDate = dueOffsetDays.HasValue ? today.AddDays(dueOffsetDays.Value) : null;

            var result = AssignmentStatusKeys.IsDueSoon(isCompleted, dueDate, today);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void CompletedHistoryRetention_UsesOneMonthSharedPolicy()
        {
            Assert.Equal(1, EnrollmentVisibilityPolicy.CompletedHistoryRetentionMonths);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(-29, true)]
        [InlineData(-31, false)]
        [InlineData(null, false)]
        public void ShouldShowCompletedEnrollment_UsesSharedRetentionCutoff(int? completedOffsetDays, bool expected)
        {
            var today = new DateTime(2026, 4, 30, 12, 0, 0, DateTimeKind.Utc);
            DateTime? completedDate = completedOffsetDays.HasValue ? today.AddDays(completedOffsetDays.Value) : null;

            var result = EnrollmentVisibilityPolicy.ShouldShowCompletedEnrollment(completedDate, today);

            Assert.Equal(expected, result);
        }
    }
}