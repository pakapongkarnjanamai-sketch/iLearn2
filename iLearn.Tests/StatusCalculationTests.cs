using iLearn.Application.Services;

namespace iLearn.Tests
{
    /// <summary>
    /// Tests for AssignmentDashboardService.CalculateStatus — the core status calculation logic
    /// covering all edge cases: snapshot vs live, expired vs completed priority, upcoming, etc.
    /// </summary>
    public class StatusCalculationTests
    {
        private static readonly DateTime Now = new(2026, 3, 10, 12, 0, 0);

        // ?? Completed ?????????????????????????????????????????????????????????
        [Fact]
        public void AllCompleted_ReturnsCompleted()
        {
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: true,
                allCompleted: true,
                startDate: Now.AddDays(-10),
                dueDate: Now.AddDays(5),
                currentDate: Now);

            Assert.Equal("Completed", result);
        }

        [Fact]
        public void AllCompleted_EvenWhenExpired_ReturnsCompleted()
        {
            // If everyone finished, "Completed" takes priority over "Expired"
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: true,
                allCompleted: true,
                startDate: Now.AddDays(-30),
                dueDate: Now.AddDays(-1),   // past due
                currentDate: Now);

            Assert.Equal("Completed", result);
        }

        [Fact]
        public void AllCompleted_EvenWhenUpcoming_ReturnsCompleted()
        {
            // Edge case: all completed before official start date
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: true,
                allCompleted: true,
                startDate: Now.AddDays(1),  // future
                dueDate: Now.AddDays(30),
                currentDate: Now);

            Assert.Equal("Completed", result);
        }

        // ?? Upcoming ??????????????????????????????????????????????????????????
        [Fact]
        public void NotCompleted_FutureStart_ReturnsUpcoming()
        {
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: true,
                allCompleted: false,
                startDate: Now.AddDays(5),
                dueDate: Now.AddDays(30),
                currentDate: Now);

            Assert.Equal("Upcoming", result);
        }

        [Fact]
        public void NoEnrollments_FutureStart_ReturnsUpcoming()
        {
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: false,
                allCompleted: false,
                startDate: Now.AddDays(1),
                dueDate: Now.AddDays(10),
                currentDate: Now);

            Assert.Equal("Upcoming", result);
        }

        // ?? Expired ???????????????????????????????????????????????????????????
        [Fact]
        public void NotCompleted_PastDue_ReturnsExpired()
        {
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: true,
                allCompleted: false,
                startDate: Now.AddDays(-30),
                dueDate: Now.AddDays(-1),
                currentDate: Now);

            Assert.Equal("Expired", result);
        }

        [Fact]
        public void NoEnrollments_PastDue_ReturnsExpired()
        {
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: false,
                allCompleted: false,
                startDate: Now.AddDays(-30),
                dueDate: Now.AddDays(-1),
                currentDate: Now);

            Assert.Equal("Expired", result);
        }

        // ?? InProgress ????????????????????????????????????????????????????????
        [Fact]
        public void NotCompleted_WithinDateRange_ReturnsInProgress()
        {
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: true,
                allCompleted: false,
                startDate: Now.AddDays(-5),
                dueDate: Now.AddDays(10),
                currentDate: Now);

            Assert.Equal("InProgress", result);
        }

        [Fact]
        public void NotCompleted_NoDates_ReturnsInProgress()
        {
            // When no start/due dates are set, default to InProgress
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: true,
                allCompleted: false,
                startDate: null,
                dueDate: null,
                currentDate: Now);

            Assert.Equal("InProgress", result);
        }

        [Fact]
        public void NotCompleted_NoStartDate_FutureDue_ReturnsInProgress()
        {
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: true,
                allCompleted: false,
                startDate: null,
                dueDate: Now.AddDays(10),
                currentDate: Now);

            Assert.Equal("InProgress", result);
        }

        [Fact]
        public void NotCompleted_NoDueDate_PastStart_ReturnsInProgress()
        {
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: true,
                allCompleted: false,
                startDate: Now.AddDays(-5),
                dueDate: null,
                currentDate: Now);

            Assert.Equal("InProgress", result);
        }

        // ?? Edge: no enrollments ??????????????????????????????????????????????
        [Fact]
        public void NoEnrollments_NoDates_ReturnsInProgress()
        {
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: false,
                allCompleted: false,
                startDate: null,
                dueDate: null,
                currentDate: Now);

            Assert.Equal("InProgress", result);
        }

        // ?? Edge: boundary (start == now, due == now) ?????????????????????????
        [Fact]
        public void StartDateEqualsNow_ReturnsInProgress()
        {
            // StartDate == now ? not "Upcoming" (must be strictly > now)
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: true,
                allCompleted: false,
                startDate: Now,
                dueDate: Now.AddDays(10),
                currentDate: Now);

            Assert.Equal("InProgress", result);
        }

        [Fact]
        public void DueDateEqualsNow_ReturnsInProgress()
        {
            // DueDate == now ? not "Expired" (must be strictly < now)
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: true,
                allCompleted: false,
                startDate: Now.AddDays(-5),
                dueDate: Now,
                currentDate: Now);

            Assert.Equal("InProgress", result);
        }

        // ?? Priority: Completed > Upcoming/Expired ????????????????????????????
        [Theory]
        [InlineData(-10, -1)]   // past due
        [InlineData(1, 10)]     // future start
        [InlineData(-10, 10)]   // in range
        public void Completed_AlwaysTakesPriority(int startDaysOffset, int dueDaysOffset)
        {
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: true,
                allCompleted: true,
                startDate: Now.AddDays(startDaysOffset),
                dueDate: Now.AddDays(dueDaysOffset),
                currentDate: Now);

            Assert.Equal("Completed", result);
        }

        // ?? Priority: Upcoming > Expired (when start is in the future but due is past — unusual but possible) ??
        [Fact]
        public void FutureStart_PastDue_ReturnsUpcoming()
        {
            // Both conditions true, but Upcoming check comes first
            var result = AssignmentDashboardService.CalculateStatus(
                hasEnrollments: false,
                allCompleted: false,
                startDate: Now.AddDays(1),
                dueDate: Now.AddDays(-1),
                currentDate: Now);

            Assert.Equal("Upcoming", result);
        }
    }
}
