using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace iLearn.Tests
{
    public class ReportServiceTests
    {
        private static readonly DateTime Now = new(2026, 7, 15, 10, 0, 0);

        #region Compliance Tests

        [Fact]
        public async Task Compliance_CountsOpenCompletedOverdue_And_DistinctLearners()
        {
            // Arrange: 3 enrollments for 2 learners
            var enrollments = new List<Enrollment>
            {
                new() { Id = 1, LearnerCode = "EMP001", IsCompleted = true, Progress = 100, CourseId = 1, StartDate = Now.AddDays(-30), DueDate = Now.AddDays(-5) },
                new() { Id = 2, LearnerCode = "EMP001", IsCompleted = false, Progress = 50, CourseId = 2, StartDate = Now.AddDays(-10), DueDate = Now.AddDays(-1) }, // overdue
                new() { Id = 3, LearnerCode = "EMP002", IsCompleted = false, Progress = 0, CourseId = 1, StartDate = Now.AddDays(-5), DueDate = Now.AddDays(10) },  // in-progress (not overdue)
            };
            var enrollmentAssignments = new List<EnrollmentAssignment>
            {
                new() { Id = 1, EnrollmentId = 1, AssignmentId = 1 },
                new() { Id = 2, EnrollmentId = 2, AssignmentId = 1 },
                new() { Id = 3, EnrollmentId = 3, AssignmentId = 2 },
            };
            var assignments = new List<Assignment>
            {
                new() { Id = 1, AssignmentNo = "ASG-001", DivisionId = null },
                new() { Id = 2, AssignmentNo = "ASG-002", DivisionId = null },
            };
            var courses = new List<Course>
            {
                new() { Id = 1, Code = "C01", Title = "Course 1", CategoryId = 1 },
                new() { Id = 2, Code = "C02", Title = "Course 2", CategoryId = 1 },
            };

            var learnerMap = new Dictionary<string, ExternalLearnerDto>(StringComparer.OrdinalIgnoreCase)
            {
                ["EMP001"] = new() { Code = "EMP001", Name = "John", Division = "IT", Department = "Dev" },
                ["EMP002"] = new() { Code = "EMP002", Name = "Jane", Division = "IT", Department = "QA" },
            };

            var service = CreateService(enrollments, enrollmentAssignments, assignments, courses, learnerMap: learnerMap);

            // Act
            var result = await service.GetComplianceReportAsync(null, Now);

            // Assert
            Assert.Equal(2, result.TotalLearners);
            Assert.Equal(1, result.CompletedEnrollments);
            Assert.Equal(2, result.OpenEnrollments);
            Assert.Equal(1, result.OverdueEnrollments);
            Assert.Equal(1, result.OverdueLearners);
            Assert.True(result.ComplianceRate > 0);
            // ComplianceRate = 1 / (1 + 2) * 100 = 33.33
            Assert.Equal(100.0 / 3.0, result.ComplianceRate, 2);
        }

        [Fact]
        public async Task Compliance_OverdueUsesStatusKeyLogic_DueDateBeforeCurrent()
        {
            // A completed enrollment with past due date should NOT be overdue
            var enrollments = new List<Enrollment>
            {
                new() { Id = 1, LearnerCode = "EMP001", IsCompleted = true, Progress = 100, CourseId = 1, DueDate = Now.AddDays(-5) },
                new() { Id = 2, LearnerCode = "EMP002", IsCompleted = false, Progress = 30, CourseId = 1, DueDate = Now.AddDays(-2) }, // overdue
            };
            var enrollmentAssignments = new List<EnrollmentAssignment>
            {
                new() { Id = 1, EnrollmentId = 1, AssignmentId = 1 },
                new() { Id = 2, EnrollmentId = 2, AssignmentId = 1 },
            };
            var assignments = new List<Assignment> { new() { Id = 1, AssignmentNo = "ASG-001" } };
            var courses = new List<Course> { new() { Id = 1, Code = "C01", Title = "Course 1", CategoryId = 1 } };

            var service = CreateService(enrollments, enrollmentAssignments, assignments, courses);

            var result = await service.GetComplianceReportAsync(null, Now);

            Assert.Equal(1, result.OverdueEnrollments);
            Assert.Single(result.OverdueRows);
            Assert.Equal("EMP002", result.OverdueRows[0].LearnerCode);
            Assert.Equal(2, result.OverdueRows[0].DaysOverdue);
        }

        [Fact]
        public async Task Compliance_UsesLinkDueDate_ExtendedLearnerIsNotOverdue()
        {
            // ExtendDueDateAsync updates only Assignment/EnrollmentAssignment.DueDate, never
            // Enrollment.DueDate — the report must honour the link date (effective schedule),
            // otherwise an extended learner shows Overdue here but In Progress on assignment pages.
            var assignment = new Assignment { Id = 1, AssignmentNo = "ASG-001" };
            var enrollment = new Enrollment
            {
                Id = 1,
                LearnerCode = "EMP001",
                IsCompleted = false,
                Progress = 40,
                CourseId = 1,
                StartDate = Now.AddDays(-30),
                DueDate = Now.AddDays(-5), // stale enrollment-level date (pre-extension)
            };
            var link = new EnrollmentAssignment
            {
                Id = 1,
                EnrollmentId = 1,
                Enrollment = enrollment,
                AssignmentId = 1,
                Assignment = assignment,
                StartDate = Now.AddDays(-30),
                DueDate = Now.AddDays(10), // extended due date lives on the link
            };
            enrollment.AssignmentLinks.Add(link);

            var service = CreateService(
                [enrollment], [link], [assignment],
                [new Course { Id = 1, Code = "C01", Title = "Course 1", CategoryId = 1 }]);

            var result = await service.GetComplianceReportAsync(null, Now);

            Assert.Equal(0, result.OverdueEnrollments);
            Assert.Empty(result.OverdueRows);
            Assert.Equal(1, result.OpenEnrollments);
        }

        [Fact]
        public async Task Compliance_ExcludesEnrollmentWhoseOnlyAssignmentIsDeleted()
        {
            // Learner side (GetEffectiveSchedule) hides enrollments whose only links point to
            // deleted assignments — the report must not count them either.
            var deletedAssignment = new Assignment { Id = 1, AssignmentNo = "ASG-DEL", IsDeleted = true };
            var hiddenEnrollment = new Enrollment
            {
                Id = 1,
                LearnerCode = "EMP001",
                IsCompleted = false,
                Progress = 0,
                CourseId = 1,
                DueDate = Now.AddDays(-3),
            };
            var deadLink = new EnrollmentAssignment
            {
                Id = 1,
                EnrollmentId = 1,
                Enrollment = hiddenEnrollment,
                AssignmentId = 1,
                Assignment = deletedAssignment,
            };
            hiddenEnrollment.AssignmentLinks.Add(deadLink);

            // Control: enrollment with no links at all stays visible (uses enrollment dates)
            var visibleEnrollment = new Enrollment
            {
                Id = 2,
                LearnerCode = "EMP002",
                IsCompleted = false,
                Progress = 20,
                CourseId = 1,
                DueDate = Now.AddDays(-1),
            };

            var service = CreateService(
                [hiddenEnrollment, visibleEnrollment], [deadLink],
                [deletedAssignment],
                [new Course { Id = 1, Code = "C01", Title = "Course 1", CategoryId = 1 }]);

            var result = await service.GetComplianceReportAsync(null, Now);

            Assert.Equal(1, result.TotalLearners);          // EMP002 only
            Assert.Equal(1, result.OpenEnrollments);
            Assert.Equal(1, result.OverdueEnrollments);      // EMP002 via enrollment-level date
            Assert.Equal("EMP002", result.OverdueRows.Single().LearnerCode);
        }

        #endregion

        #region Transcript Tests

        [Fact]
        public async Task Transcript_LearnerWith3Enrollments_ReturnsFullRowsAndStats()
        {
            var enrollments = new List<Enrollment>
            {
                new() { Id = 1, LearnerCode = "EMP001", IsCompleted = true, Progress = 100, TotalScore = 85, TotalTimeSpent = 3600, CourseId = 1, StartDate = Now.AddDays(-30), DueDate = Now.AddDays(-10), CompletedDate = Now.AddDays(-12) },
                new() { Id = 2, LearnerCode = "EMP001", IsCompleted = false, Progress = 60, TotalScore = 0, TotalTimeSpent = 1800, CourseId = 2, StartDate = Now.AddDays(-10), DueDate = Now.AddDays(5) },
                new() { Id = 3, LearnerCode = "EMP001", IsCompleted = true, Progress = 100, TotalScore = 90, TotalTimeSpent = 7200, CourseId = 3, StartDate = Now.AddDays(-60), DueDate = Now.AddDays(-30), CompletedDate = Now.AddDays(-35) },
            };
            var enrollmentAssignments = new List<EnrollmentAssignment>
            {
                new() { Id = 1, EnrollmentId = 1, AssignmentId = 1 },
                new() { Id = 2, EnrollmentId = 2, AssignmentId = 2 },
                new() { Id = 3, EnrollmentId = 3, AssignmentId = 1 },
            };
            var assignments = new List<Assignment>
            {
                new() { Id = 1, AssignmentNo = "ASG-001" },
                new() { Id = 2, AssignmentNo = "ASG-002" },
            };
            var courses = new List<Course>
            {
                new() { Id = 1, Code = "C01", Title = "Course 1", CategoryId = 1 },
                new() { Id = 2, Code = "C02", Title = "Course 2", CategoryId = 1 },
                new() { Id = 3, Code = "C03", Title = "Course 3", CategoryId = 1 },
            };
            var learnerMap = new Dictionary<string, ExternalLearnerDto>(StringComparer.OrdinalIgnoreCase)
            {
                ["EMP001"] = new() { Code = "EMP001", Name = "John", Division = "IT", Department = "Dev" },
            };
            var groupMembers = new List<LearnerGroupMember>
            {
                new() { Id = 1, LearnerGroupId = 10, LearnerCode = "EMP001" },
            };
            var groups = new List<LearnerGroup>
            {
                new() { Id = 10, Name = "IT Team" },
            };

            var service = CreateService(enrollments, enrollmentAssignments, assignments, courses,
                learnerMap: learnerMap, groupMembers: groupMembers, learnerGroups: groups);

            var result = await service.GetTranscriptReportAsync("EMP001", null, Now);

            Assert.Equal("EMP001", result.LearnerCode);
            Assert.Equal("John", result.LearnerName);
            Assert.Equal("IT", result.Division);
            Assert.Equal(3, result.TotalCourses);
            Assert.Equal(2, result.CompletedCourses);
            Assert.Equal(3, result.Rows.Count);
            Assert.Contains("IT Team", result.LearnerGroups);

            // Check status calculation
            var completedRow = result.Rows.First(r => r.EnrollmentId == 1);
            Assert.Equal(AssignmentStatusKeys.Learner.Completed, completedRow.Status);

            var inProgressRow = result.Rows.First(r => r.EnrollmentId == 2);
            Assert.Equal(AssignmentStatusKeys.Learner.InProgress, inProgressRow.Status);
        }

        [Fact]
        public async Task Transcript_LearnerNotFound_ThrowsKeyNotFoundException()
        {
            var service = CreateService([], [], [], []);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.GetTranscriptReportAsync("UNKNOWN", null, Now));
        }

        #endregion

        #region Activity Tests

        [Fact]
        public async Task Activity_MonthsWithNoData_HaveZeroRows()
        {
            // Only one enrollment completed in July 2026
            var enrollments = new List<Enrollment>
            {
                new() { Id = 1, LearnerCode = "EMP001", IsCompleted = true, Progress = 100, CourseId = 1, CompletedDate = new DateTime(2026, 7, 10), CreatedAt = new DateTime(2026, 5, 1) },
            };
            var enrollmentAssignments = new List<EnrollmentAssignment>
            {
                new() { Id = 1, EnrollmentId = 1, AssignmentId = 1 },
            };
            var assignments = new List<Assignment> { new() { Id = 1, AssignmentNo = "ASG-001" } };
            var courses = new List<Course> { new() { Id = 1, Code = "C01", Title = "Course 1", CategoryId = 1 } };

            var service = CreateService(enrollments, enrollmentAssignments, assignments, courses);

            var result = await service.GetActivityReportAsync(6, null);

            // 6 months from Feb 2026 to Jul 2026
            Assert.Equal(6, result.Months.Count);

            // July should have 1 completion
            var julyRow = result.Months.First(m => m.Month == "2026-07");
            Assert.Equal(1, julyRow.Completions);

            // May should have 1 new enrollment (CreatedAt)
            var mayRow = result.Months.First(m => m.Month == "2026-05");
            Assert.Equal(1, mayRow.NewEnrollments);

            // Feb, Mar, Apr should have zeros
            var febRow = result.Months.First(m => m.Month == "2026-02");
            Assert.Equal(0, febRow.Completions);
            Assert.Equal(0, febRow.NewEnrollments);
            Assert.Equal(0, febRow.ActiveLearners);
        }

        [Fact]
        public async Task Activity_CompletionsCountedFromCompletedDate()
        {
            var enrollments = new List<Enrollment>
            {
                new() { Id = 1, LearnerCode = "EMP001", IsCompleted = true, CourseId = 1, CompletedDate = new DateTime(2026, 6, 15), CreatedAt = new DateTime(2026, 4, 1) },
                new() { Id = 2, LearnerCode = "EMP002", IsCompleted = true, CourseId = 1, CompletedDate = new DateTime(2026, 6, 20), CreatedAt = new DateTime(2026, 4, 5) },
                new() { Id = 3, LearnerCode = "EMP003", IsCompleted = false, CourseId = 1, CreatedAt = new DateTime(2026, 6, 1) }, // not completed — should not be counted
            };
            var enrollmentAssignments = new List<EnrollmentAssignment>
            {
                new() { Id = 1, EnrollmentId = 1, AssignmentId = 1 },
                new() { Id = 2, EnrollmentId = 2, AssignmentId = 1 },
                new() { Id = 3, EnrollmentId = 3, AssignmentId = 1 },
            };
            var assignments = new List<Assignment> { new() { Id = 1, AssignmentNo = "ASG-001" } };
            var courses = new List<Course> { new() { Id = 1, Code = "C01", Title = "Course 1", CategoryId = 1 } };

            var service = CreateService(enrollments, enrollmentAssignments, assignments, courses);

            var result = await service.GetActivityReportAsync(6, null);

            var juneRow = result.Months.First(m => m.Month == "2026-06");
            Assert.Equal(2, juneRow.Completions);
        }

        #endregion

        #region Course Summary Tests

        [Fact]
        public async Task CourseSummary_DistinctLearners_And_AvgCalculations()
        {
            // Course 1: 2 enrollments (same learner twice + different learner), 1 completed
            var enrollments = new List<Enrollment>
            {
                new() { Id = 1, LearnerCode = "EMP001", IsCompleted = true, Progress = 100, TotalScore = 80, CourseId = 1, DueDate = Now.AddDays(-5) },
                new() { Id = 2, LearnerCode = "EMP001", IsCompleted = false, Progress = 50, TotalScore = 0, CourseId = 1, DueDate = Now.AddDays(5) },
                new() { Id = 3, LearnerCode = "EMP002", IsCompleted = false, Progress = 30, TotalScore = 0, CourseId = 1, DueDate = Now.AddDays(-1) }, // overdue
                new() { Id = 4, LearnerCode = "EMP003", IsCompleted = true, Progress = 100, TotalScore = 90, CourseId = 2, DueDate = Now.AddDays(-10) },
            };
            var enrollmentAssignments = new List<EnrollmentAssignment>
            {
                new() { Id = 1, EnrollmentId = 1, AssignmentId = 1 },
                new() { Id = 2, EnrollmentId = 2, AssignmentId = 2 },
                new() { Id = 3, EnrollmentId = 3, AssignmentId = 1 },
                new() { Id = 4, EnrollmentId = 4, AssignmentId = 3 },
            };
            var assignments = new List<Assignment>
            {
                new() { Id = 1, AssignmentNo = "ASG-001", CourseId = 1 },
                new() { Id = 2, AssignmentNo = "ASG-002", CourseId = 1 },
                new() { Id = 3, AssignmentNo = "ASG-003", CourseId = 2 },
            };
            var courses = new List<Course>
            {
                new() { Id = 1, Code = "C01", Title = "Course 1", CategoryId = 1, Category = new Category { Id = 1, Name = "General" } },
                new() { Id = 2, Code = "C02", Title = "Course 2", CategoryId = 1, Category = new Category { Id = 1, Name = "General" } },
            };

            var service = CreateService(enrollments, enrollmentAssignments, assignments, courses);

            var result = await service.GetCourseSummaryReportAsync(null, Now);

            Assert.Equal(2, result.Rows.Count);

            var course1Row = result.Rows.First(r => r.CourseId == 1);
            Assert.Equal(2, course1Row.EnrolledLearners); // EMP001 + EMP002
            Assert.Equal(1, course1Row.CompletedCount);
            Assert.Equal(1, course1Row.OverdueCount); // EMP002's enrollment is overdue
            Assert.Equal(80.0, course1Row.AvgScore); // only score > 0 counts

            var course2Row = result.Rows.First(r => r.CourseId == 2);
            Assert.Equal(1, course2Row.EnrolledLearners);
            Assert.Equal(1, course2Row.CompletedCount);
            Assert.Equal(100.0, course2Row.CompletionRate);
        }

        #endregion

        #region Test Infrastructure

        private ReportService CreateService(
            List<Enrollment> enrollments,
            List<EnrollmentAssignment> enrollmentAssignments,
            List<Assignment> assignments,
            List<Course> courses,
            List<LearningLog>? learningLogs = null,
            Dictionary<string, ExternalLearnerDto>? learnerMap = null,
            List<LearnerGroupMember>? groupMembers = null,
            List<LearnerGroup>? learnerGroups = null)
        {
            return new ReportService(
                new InMemoryGenericRepository<Enrollment>(enrollments, Now),
                new InMemoryGenericRepository<EnrollmentAssignment>(enrollmentAssignments, Now),
                new InMemoryGenericRepository<Assignment>(assignments, Now),
                new InMemoryGenericRepository<Course>(courses, Now),
                new InMemoryGenericRepository<LearningLog>(learningLogs ?? [], Now),
                new InMemoryGenericRepository<LearnerGroupMember>(groupMembers ?? [], Now),
                new InMemoryGenericRepository<LearnerGroup>(learnerGroups ?? [], Now),
                new FakeLearnerApiService(learnerMap ?? new()),
                new FakeDateTime(Now));
        }

        private sealed class FakeDateTime : IDateTime
        {
            public FakeDateTime(DateTime now) { Now = now; }
            public DateTime Now { get; }
            public CultureInfo CultureInfo => CultureInfo.InvariantCulture;
            public DateTime UnixTime => Now;
        }

        private sealed class FakeLearnerApiService : ILearnerApiService
        {
            private readonly Dictionary<string, ExternalLearnerDto> _map;

            public FakeLearnerApiService(Dictionary<string, ExternalLearnerDto> map)
            {
                _map = new Dictionary<string, ExternalLearnerDto>(map, StringComparer.OrdinalIgnoreCase);
            }

            public Task<Dictionary<string, ExternalLearnerDto>> GetLearnersByCodesAsync(IEnumerable<string> codes)
            {
                var result = new Dictionary<string, ExternalLearnerDto>(StringComparer.OrdinalIgnoreCase);
                foreach (var code in codes)
                {
                    if (_map.TryGetValue(code, out var dto))
                        result[code] = dto;
                }
                return Task.FromResult(result);
            }

            public Task<ExternalLearnerDto> GetLearnerByCodeAsync(string Code) => throw new NotImplementedException();
            public Task<AllLearnersApiResponse> GetLearnerAsync() => throw new NotImplementedException();
            public Task<DivisionApiResponse> GetLearnersByDivisionsAsync(string[] divisions, int skip = 0, int take = 20) => throw new NotImplementedException();
            public Task<string> GetLearnersDxGridAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetSectionsAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetDivisionsAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetDepartmentsAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetPositionsAsync(string queryString) => throw new NotImplementedException();
            public Task<Dictionary<string, EmployeeCsvDto>> GetEmployeesByNidsAsync(IEnumerable<string> nids) => throw new NotImplementedException();
        }

        private class InMemoryGenericRepository<T> : IGenericRepository<T> where T : BaseEntity
        {
            private readonly DateTime _now;
            private int _nextId;

            public InMemoryGenericRepository(IEnumerable<T> items, DateTime now)
            {
                Items = items.ToList();
                _now = now;
                _nextId = Items.Count == 0 ? 1 : Items.Max(x => x.Id) + 1;
            }

            public List<T> Items { get; }

            public Task<T> AddAsync(T entity)
            {
                AddEntity(entity);
                return Task.FromResult(entity);
            }

            public Task<T> AddWithoutSaveAsync(T entity)
            {
                AddEntity(entity);
                return Task.FromResult(entity);
            }

            public Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
            {
                return Task.FromResult(ApplyFilter(filter).Count());
            }

            public Task DeleteAsync(T entity)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = _now;
                return Task.CompletedTask;
            }

            public void DeleteWithoutSave(T entity)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = _now;
            }

            public Task<IReadOnlyList<T>> GetAllAsync()
            {
                return Task.FromResult<IReadOnlyList<T>>(Items.Where(x => !x.IsDeleted).ToList());
            }

            public Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, bool ignoreQueryFilters = false)
            {
                var result = ApplyFilter(filter, ignoreQueryFilters).ToList();
                return Task.FromResult<IReadOnlyList<T>>(result);
            }

            public Task<IEnumerable<TResult>> GetAsync<TResult>(Expression<Func<T, bool>>? filter = null, Expression<Func<T, TResult>>? selector = null)
            {
                if (selector == null)
                    throw new ArgumentException("Selector is required", nameof(selector));
                var result = ApplyFilter(filter).Select(selector.Compile()).ToList();
                return Task.FromResult<IEnumerable<TResult>>(result);
            }

            public Task<T?> GetByIdAsync(int id)
            {
                return Task.FromResult(Items.FirstOrDefault(x => x.Id == id && !x.IsDeleted));
            }

            public IQueryable<T> GetQuery()
            {
                return new TestAsyncEnumerable<T>(Items.Where(x => !x.IsDeleted).AsQueryable());
            }

            public Task HardDeleteAsync(T entity)
            {
                Items.Remove(entity);
                return Task.CompletedTask;
            }

            public Task UpdateAsync(T entity) => Task.CompletedTask;
            public void UpdateWithoutSave(T entity) { }

            private IEnumerable<T> ApplyFilter(Expression<Func<T, bool>>? filter, bool ignoreQueryFilters = false)
            {
                var query = ignoreQueryFilters ? Items.AsEnumerable() : Items.Where(x => !x.IsDeleted);
                return filter == null ? query : query.Where(filter.Compile());
            }

            private void AddEntity(T entity)
            {
                if (entity.Id == 0) entity.Id = _nextId++;
                if (!Items.Contains(entity)) Items.Add(entity);
            }
        }

        #endregion

        #region Async Queryable Infrastructure

        private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public TestAsyncEnumerable(IQueryable<T> queryable)
                : base(queryable.Expression)
            {
                Provider = new TestAsyncQueryProvider<T>(queryable.Provider);
            }

            public TestAsyncEnumerable(Expression expression)
                : base(expression)
            {
                Provider = new TestAsyncQueryProvider<T>(this);
            }

            IQueryProvider IQueryable.Provider => Provider;
            public new IQueryProvider Provider { get; }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            }
        }

        private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider, IQueryProvider
        {
            private readonly IQueryProvider _inner;

            public TestAsyncQueryProvider(IQueryProvider inner) { _inner = inner; }

            public IQueryable CreateQuery(Expression expression)
                => new TestAsyncEnumerable<TEntity>(expression);

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
                => new TestAsyncEnumerable<TElement>(expression);

            public object? Execute(Expression expression) => _inner.Execute(expression);
            public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

            public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
            {
                var resultType = typeof(TResult).GetGenericArguments()[0];
                var executeMethod = typeof(IQueryProvider)
                    .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!
                    .MakeGenericMethod(resultType);
                var result = executeMethod.Invoke(_inner, [expression]);
                return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(resultType)
                    .Invoke(null, [result])!;
            }
        }

        private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;

            public TestAsyncEnumerator(IEnumerator<T> inner) { _inner = inner; }

            public T Current => _inner.Current;

            public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
            public ValueTask DisposeAsync()
            {
                _inner.Dispose();
                return ValueTask.CompletedTask;
            }
        }

        #endregion
    }
}
