using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using System.Linq.Expressions;

namespace iLearn.Tests
{
    public class AssignmentFlowTests
    {
        private static readonly DateTime Now = new(2026, 3, 20, 9, 0, 0);

        [Fact]
        public async Task AssignCoursesToEmployees_CreatesEnrollmentAndAssignmentLink()
        {
            var course = new Course
            {
                Id = 10,
                IsActive = true,
                Code = "C-10",
                Title = "Course 10"
            };
            var version = new CourseVersion
            {
                Id = 100,
                CourseId = 10,
                VersionNumber = 1,
                IsActive = true
            };

            var service = CreateCourseAssignmentService(
                courses: [course],
                enrollments: [],
                enrollmentAssignments: [],
                assignments: [],
                versions: [version]);

            await service.AssignCoursesToEmployees(
                new Dictionary<int, int> { [10] = 9001 },
                ["490222"],
                Now,
                Now.AddDays(7),
                forceReset: false);

            var enrollment = service.EnrollmentRepository.Items.Single();
            Assert.Equal("490222", enrollment.StudentCode);
            Assert.Equal(10, enrollment.CourseId);
            Assert.Equal(100, enrollment.EnrolledCourseVersion);

            var link = service.EnrollmentAssignmentRepository.Items.Single();
            Assert.Equal(9001, link.AssignmentId);
            Assert.Same(enrollment, link.Enrollment);
            Assert.Equal(Now, link.StartDate);
            Assert.Equal(Now.AddDays(7), link.DueDate);
        }

        [Fact]
        public async Task AssignCoursesToEmployees_ReassignCompletedEnrollment_ResetsEnrollmentAndSnapshotsExistingLinks()
        {
            var course = new Course
            {
                Id = 20,
                IsActive = true,
                Code = "C-20",
                Title = "Course 20"
            };
            var oldVersion = new CourseVersion
            {
                Id = 200,
                CourseId = 20,
                VersionNumber = 1,
                IsActive = false
            };
            var activeVersion = new CourseVersion
            {
                Id = 201,
                CourseId = 20,
                VersionNumber = 2,
                IsActive = true
            };
            var enrollment = new Enrollment
            {
                Id = 1,
                StudentCode = "490222",
                CourseId = 20,
                EnrolledCourseVersion = 200,
                IsCompleted = true,
                CompletedDate = Now.AddDays(-2),
                Progress = 100,
                TotalScore = 85,
                StartDate = Now.AddDays(-10),
                DueDate = Now.AddDays(5)
            };
            var existingLink = new EnrollmentAssignment
            {
                Id = 7,
                EnrollmentId = 1,
                Enrollment = enrollment,
                AssignmentId = 8001,
                StartDate = Now.AddDays(-10),
                DueDate = Now.AddDays(5)
            };

            var service = CreateCourseAssignmentService(
                courses: [course],
                enrollments: [enrollment],
                enrollmentAssignments: [existingLink],
                assignments: [],
                versions: [oldVersion, activeVersion]);

            await service.AssignCoursesToEmployees(
                new Dictionary<int, int> { [20] = 8002 },
                ["490222"],
                Now,
                Now.AddDays(14),
                forceReset: true);

            Assert.False(enrollment.IsCompleted);
            Assert.Null(enrollment.CompletedDate);
            Assert.Equal(0, enrollment.Progress);
            Assert.Equal(0, enrollment.TotalScore);
            Assert.Equal(201, enrollment.EnrolledCourseVersion);
            Assert.Equal(Now, enrollment.ResetAt);

            Assert.True(existingLink.SnapshotCompleted);
            Assert.Equal(100, existingLink.SnapshotProgress);
            Assert.Equal(Now.AddDays(-2), existingLink.SnapshotCompletedDate);

            var newLink = service.EnrollmentAssignmentRepository.Items.Single(ea => ea.AssignmentId == 8002);
            Assert.Same(enrollment, newLink.Enrollment);
            Assert.Equal(Now, newLink.StartDate);
            Assert.Equal(Now.AddDays(14), newLink.DueDate);
        }

        [Fact]
        public async Task AssignmentBatchService_LoadBatchAsync_UsesAssignmentNoAcrossBatch()
        {
            var repo = new InMemoryGenericRepository<Assignment>(
            [
                new Assignment { Id = 1, AssignmentNo = "AS-20260320-001", DivisionId = 7 },
                new Assignment { Id = 2, AssignmentNo = "AS-20260320-001", DivisionId = 7 },
                new Assignment { Id = 3, AssignmentNo = "AS-20260320-002", DivisionId = 7 }
            ],
            Now);

            var service = new AssignmentBatchService(repo, new FakeCurrentUserService { DivisionId = 7 });

            var result = await service.LoadBatchAsync(repo.Items.First(a => a.Id == 1));

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal("AS-20260320-001", r.AssignmentNo));
        }

        [Fact]
        public async Task AssignmentBatchService_LoadBatchAsync_FallsBackToAssignmentId_WhenAssignmentNoMissing()
        {
            var repo = new InMemoryGenericRepository<Assignment>(
            [
                new Assignment { Id = 11, AssignmentNo = null, DivisionId = 3 },
                new Assignment { Id = 12, AssignmentNo = null, DivisionId = 3 }
            ],
            Now);

            var service = new AssignmentBatchService(repo, new FakeCurrentUserService { DivisionId = 3 });
            var assignment = repo.Items.First(a => a.Id == 11);

            var result = await service.LoadBatchAsync(assignment);

            Assert.Single(result);
            Assert.Equal(11, result[0].Id);
            Assert.Equal("assignment:11", service.GetBatchKey(assignment));
        }

        private static CourseAssignmentServiceHarness CreateCourseAssignmentService(
            IEnumerable<Course> courses,
            IEnumerable<Enrollment> enrollments,
            IEnumerable<EnrollmentAssignment> enrollmentAssignments,
            IEnumerable<Assignment> assignments,
            IEnumerable<CourseVersion> versions)
        {
            var courseRepo = new InMemoryCourseRepository(courses, Now);
            var enrollmentRepo = new InMemoryGenericRepository<Enrollment>(enrollments, Now);
            var enrollmentAssignmentRepo = new InMemoryGenericRepository<EnrollmentAssignment>(enrollmentAssignments, Now);
            var assignmentRepo = new InMemoryGenericRepository<Assignment>(assignments, Now);
            var versionRepo = new InMemoryGenericRepository<CourseVersion>(versions, Now);
            var unitOfWork = new FakeUnitOfWork();

            var service = new CourseAssignmentService(
                courseRepo,
                enrollmentRepo,
                enrollmentAssignmentRepo,
                assignmentRepo,
                new FakeAssignmentDashboardService(),
                versionRepo,
                new FakeDateTime(Now),
                unitOfWork);

            return new CourseAssignmentServiceHarness(service, enrollmentRepo, enrollmentAssignmentRepo);
        }

        private sealed record CourseAssignmentServiceHarness(
            CourseAssignmentService Service,
            InMemoryGenericRepository<Enrollment> EnrollmentRepository,
            InMemoryGenericRepository<EnrollmentAssignment> EnrollmentAssignmentRepository)
        {
            public Task AssignCoursesToEmployees(
                IReadOnlyDictionary<int, int> assignmentRuleIdsByCourseId,
                List<string> employeeCodes,
                DateTime? startDate,
                DateTime? dueDate,
                bool forceReset = false)
            {
                return Service.AssignCoursesToEmployees(assignmentRuleIdsByCourseId, employeeCodes, startDate, dueDate, forceReset);
            }
        }

        private sealed class FakeAssignmentDashboardService : IAssignmentDashboardService
        {
            public Task<AssignmentDashboardDto?> GetDashboardAsync(int assignmentId) => Task.FromResult<AssignmentDashboardDto?>(null);

            public Task<ValidateBeforeAssignResult> ValidateBeforeAssignAsync(BulkAssignDto dto) =>
                Task.FromResult(new ValidateBeforeAssignResult { Success = true, ResolvedCount = dto.EmployeeCodes.Count });

            public Task<PagedResult<AssignmentHistoryDto>> GetAssignmentHistoryPagedAsync(PaginationParams p) =>
                Task.FromResult(new PagedResult<AssignmentHistoryDto>());

            public Task<List<AssignmentGroupHistoryDto>> GetGroupHistoryAsync(int groupId) =>
                Task.FromResult(new List<AssignmentGroupHistoryDto>());

            public Task ExtendDueDateAsync(int assignmentId, DateTime newDueDate) => Task.CompletedTask;

            public Task<List<LookupCourseDto>> GetLookupCoursesAsync() =>
                Task.FromResult(new List<LookupCourseDto>());
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public string UserId => "tester";
            public string FullName => "tester";
            public bool IsAuthenticated => true;
            public int? DivisionId { get; init; }
            public string? DivisionName => "QA";
            public bool IsSuperAdmin => !DivisionId.HasValue;
        }

        private sealed class FakeDateTime : IDateTime
        {
            public FakeDateTime(DateTime now)
            {
                Now = now;
            }

            public DateTime Now { get; }
            public System.Globalization.CultureInfo CultureInfo => System.Globalization.CultureInfo.InvariantCulture;
            public DateTime UnixTime => Now;
        }

        private sealed class FakeUnitOfWork : IUnitOfWork
        {
            public int SaveCallCount { get; private set; }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveCallCount++;
                return Task.FromResult(0);
            }

            public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
                => throw new NotSupportedException("Transactions are not exercised by these unit tests.");

            public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
                where T : iLearn.Domain.Common.BaseEntity
                => Task.CompletedTask;

            public void Dispose()
            {
            }
        }

        private sealed class InMemoryCourseRepository : InMemoryGenericRepository<Course>, ICourseRepository
        {
            public InMemoryCourseRepository(IEnumerable<Course> items, DateTime now)
                : base(items, now)
            {
            }

            public Task<IEnumerable<Course>> GetActiveCoursesAsync()
            {
                return Task.FromResult<IEnumerable<Course>>(Items.Where(c => c.IsActive).ToList());
            }

            public Task<bool> IsCourseCodeUniqueAsync(string code)
            {
                return Task.FromResult(!Items.Any(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase)));
            }
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
                DeleteWithoutSave(entity);
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
                return Items.Where(x => !x.IsDeleted).AsQueryable();
            }

            public Task HardDeleteAsync(T entity)
            {
                Items.Remove(entity);
                return Task.CompletedTask;
            }

            public Task UpdateAsync(T entity)
            {
                UpdateWithoutSave(entity);
                return Task.CompletedTask;
            }

            public void UpdateWithoutSave(T entity)
            {
                if (!Items.Contains(entity) && entity.Id != 0)
                {
                    Items.Add(entity);
                }
            }

            private IEnumerable<T> ApplyFilter(Expression<Func<T, bool>>? filter, bool ignoreQueryFilters = false)
            {
                var query = ignoreQueryFilters ? Items.AsEnumerable() : Items.Where(x => !x.IsDeleted);
                return filter == null ? query : query.Where(filter.Compile());
            }

            private void AddEntity(T entity)
            {
                if (entity.Id == 0)
                {
                    entity.Id = _nextId++;
                }

                if (!Items.Contains(entity))
                {
                    Items.Add(entity);
                }
            }
        }
    }
}
