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
    public sealed class CourseVersionLearnerPolicyTests
    {
        private static readonly DateTime Now = new(2026, 4, 28, 10, 30, 0);

        [Fact]
        public async Task GetVersionLearnerImpactAsync_CountsOnlyOpenLearnersInInProgressAssignments()
        {
            var harness = CreatePolicyHarness();

            var impact = await harness.Service.GetVersionLearnerImpactAsync(10);

            Assert.Equal(1, impact.NotStartedCount);
            Assert.Equal(2, impact.InProgressCount);
            Assert.Equal(1, impact.CompletedCount);
            Assert.Equal(2, impact.OtherOpenCount);
        }

        [Fact]
        public async Task CreateVersionAsync_MoveNotStarted_MovesOnlyNotStartedLearners()
        {
            var harness = CreatePolicyHarness();

            var version = await harness.Service.CreateVersionAsync(10, new CreateCourseVersionDto
            {
                Note = "Version 2",
                IsActive = true,
                LearnerPolicy = CourseVersionLearnerPolicy.MoveNotStarted,
                ResourceIds = [500],
                ResourceTypes = [1]
            }, []);

            var notStarted = harness.Enrollments.Items.Single(e => e.Id == 1);
            var inProgressByProgress = harness.Enrollments.Items.Single(e => e.Id == 2);
            var completed = harness.Enrollments.Items.Single(e => e.Id == 3);
            var upcoming = harness.Enrollments.Items.Single(e => e.Id == 4);
            var notAssigned = harness.Enrollments.Items.Single(e => e.Id == 5);
            var inProgressByLog = harness.Enrollments.Items.Single(e => e.Id == 6);

            Assert.Equal(version.Id, notStarted.EnrolledCourseVersion);
            Assert.Equal(0, notStarted.Progress);
            Assert.NotNull(notStarted.ResetAt);

            Assert.Equal(100, inProgressByProgress.EnrolledCourseVersion);
            Assert.Equal(100, completed.EnrolledCourseVersion);
            Assert.Equal(100, upcoming.EnrolledCourseVersion);
            Assert.Equal(100, notAssigned.EnrolledCourseVersion);
            Assert.Equal(100, inProgressByLog.EnrolledCourseVersion);
        }

        [Fact]
        public async Task SetActiveVersionAsync_ResetInProgress_MovesOpenLearnersAndKeepsCompleted()
        {
            var harness = CreatePolicyHarness(includeInactiveVersion: true);

            await harness.Service.SetActiveVersionAsync(10, 101, CourseVersionLearnerPolicy.ResetInProgress);

            var notStarted = harness.Enrollments.Items.Single(e => e.Id == 1);
            var inProgressByProgress = harness.Enrollments.Items.Single(e => e.Id == 2);
            var completed = harness.Enrollments.Items.Single(e => e.Id == 3);
            var upcoming = harness.Enrollments.Items.Single(e => e.Id == 4);
            var notAssigned = harness.Enrollments.Items.Single(e => e.Id == 5);
            var inProgressByLog = harness.Enrollments.Items.Single(e => e.Id == 6);

            Assert.Equal(101, notStarted.EnrolledCourseVersion);
            Assert.Equal(101, inProgressByProgress.EnrolledCourseVersion);
            Assert.Equal(101, inProgressByLog.EnrolledCourseVersion);
            Assert.Equal(0, inProgressByProgress.Progress);
            Assert.NotNull(inProgressByProgress.ResetAt);

            Assert.Equal(100, completed.EnrolledCourseVersion);
            Assert.True(completed.IsCompleted);
            Assert.Equal(100, upcoming.EnrolledCourseVersion);
            Assert.Equal(100, notAssigned.EnrolledCourseVersion);
        }

        private static CourseVersionPolicyHarness CreatePolicyHarness(bool includeInactiveVersion = false)
        {
            var course = new Course { Id = 10, Code = "C-10", Title = "Course 10", IsActive = true };
            var oldVersion = new CourseVersion { Id = 100, CourseId = 10, VersionNumber = 1, IsActive = true };
            var inactiveVersion = new CourseVersion { Id = 101, CourseId = 10, VersionNumber = 2, IsActive = false };
            var resource = new Resource { Id = 500, Name = "learn.zip", TypeId = 1, IsActive = true };
            var inProgressAssignment = new Assignment
            {
                Id = 700,
                CourseId = 10,
                StartDate = Now.AddDays(-1),
                DueDate = Now.AddDays(7)
            };
            var upcomingAssignment = new Assignment
            {
                Id = 701,
                CourseId = 10,
                StartDate = Now.AddDays(1),
                DueDate = Now.AddDays(7)
            };

            var enrollments = new List<Enrollment>
            {
                new() { Id = 1, StudentCode = "490001", CourseId = 10, EnrolledCourseVersion = 100, Progress = 0 },
                new() { Id = 2, StudentCode = "490002", CourseId = 10, EnrolledCourseVersion = 100, Progress = 40, TotalScore = 12 },
                new() { Id = 3, StudentCode = "490003", CourseId = 10, EnrolledCourseVersion = 100, Progress = 100, IsCompleted = true, CompletedDate = Now.AddDays(-2) },
                new() { Id = 4, StudentCode = "490004", CourseId = 10, EnrolledCourseVersion = 100, Progress = 0 },
                new() { Id = 5, StudentCode = "490005", CourseId = 10, EnrolledCourseVersion = 100, Progress = 0 },
                new() { Id = 6, StudentCode = "490006", CourseId = 10, EnrolledCourseVersion = 100, Progress = 0 }
            };

            var enrollmentAssignments = new List<EnrollmentAssignment>
            {
                CreateLink(1, enrollments[0], inProgressAssignment),
                CreateLink(2, enrollments[1], inProgressAssignment),
                CreateLink(3, enrollments[2], inProgressAssignment),
                CreateLink(4, enrollments[3], upcomingAssignment),
                CreateLink(6, enrollments[5], inProgressAssignment)
            };

            var courseResource = new CourseResource
            {
                Id = 50,
                CourseVersionId = inactiveVersion.Id,
                CourseVersion = inactiveVersion,
                ResourceId = resource.Id,
                Resource = resource,
                Order = 1
            };

            var versionRepo = new InMemoryGenericRepository<CourseVersion>(
                includeInactiveVersion ? [oldVersion, inactiveVersion] : [oldVersion], Now);
            var courseResourceRepo = new InMemoryGenericRepository<CourseResource>(
                includeInactiveVersion ? [courseResource] : [], Now);
            var resourceRepo = new InMemoryGenericRepository<Resource>([resource], Now);
            var fileStorageRepo = new InMemoryGenericRepository<FileStorage>([], Now);
            var enrollmentRepo = new InMemoryGenericRepository<Enrollment>(enrollments, Now);
            var enrollmentAssignmentRepo = new InMemoryGenericRepository<EnrollmentAssignment>(enrollmentAssignments, Now);
            var learningLogRepo = new InMemoryGenericRepository<LearningLog>(
            [
                new LearningLog
                {
                    Id = 90,
                    EnrollmentId = 6,
                    StudentCode = "490006",
                    CourseVersionId = 100,
                    ResourceId = 500,
                    Status = "incomplete",
                    Progress = 0,
                    CreatedAt = Now.AddMinutes(-5)
                }
            ], Now);
            var unitOfWork = new FakeUnitOfWork();

            var service = new CourseVersionService(
                versionRepo,
                courseResourceRepo,
                resourceRepo,
                fileStorageRepo,
                enrollmentRepo,
                enrollmentAssignmentRepo,
                learningLogRepo,
                new InMemoryCourseRepository([course], Now),
                new FakeScormService(),
                new FakeAdminActivityService(),
                new FakeCurrentUserService(),
                new FakeDateTime(Now),
                unitOfWork);

            return new CourseVersionPolicyHarness(service, enrollmentRepo);
        }

        private static EnrollmentAssignment CreateLink(int enrollmentId, Enrollment enrollment, Assignment assignment)
        {
            return new EnrollmentAssignment
            {
                Id = enrollmentId,
                EnrollmentId = enrollment.Id,
                Enrollment = enrollment,
                AssignmentId = assignment.Id,
                Assignment = assignment,
                StartDate = assignment.StartDate,
                DueDate = assignment.DueDate
            };
        }

        private sealed record CourseVersionPolicyHarness(
            CourseVersionService Service,
            InMemoryGenericRepository<Enrollment> Enrollments);

        private sealed class FakeScormService : IScormService
        {
            public Task<ScormManifestDto> ExtractAndParseScormAsync(byte[] fileContent, string folderName) =>
                Task.FromResult(new ScormManifestDto { FolderName = folderName });

            public void DeleteScormFolder(string folderName)
            {
            }

            public string GetScormUrl(string folderName, string resourceHref) => folderName;

            public (int FileCount, long TotalSize) GetFolderInfo(string folderName) => (0, 0);
        }

        private sealed class FakeAdminActivityService : IAdminActivityService
        {
            public Task<IReadOnlyList<AdminActivityDto>> GetRecentActivitiesAsync(int take = 20, int? divisionId = null) =>
                Task.FromResult<IReadOnlyList<AdminActivityDto>>([]);

            public Task LogAsync(string actionType, string entityType, int? entityId, string title, string? description = null, int? divisionId = null, string? dataJson = null) =>
                Task.CompletedTask;
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public string UserId => "tester";
            public string FullName => "tester";
            public bool IsAuthenticated => true;
            public int? DivisionId => null;
            public string? DivisionName => "QA";
            public bool IsSuperAdmin => true;
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
            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

            public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
                throw new NotSupportedException("Transactions are not exercised by these unit tests.");

            public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
                where T : BaseEntity => Task.CompletedTask;

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

            public Task<IEnumerable<Course>> GetActiveCoursesAsync() =>
                Task.FromResult<IEnumerable<Course>>(Items.Where(c => c.IsActive).ToList());

            public Task<bool> IsCourseCodeUniqueAsync(string code) =>
                Task.FromResult(!Items.Any(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase)));
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

            public Task<int> CountAsync(Expression<Func<T, bool>>? filter = null) =>
                Task.FromResult(ApplyFilter(filter).Count());

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

            public Task<IReadOnlyList<T>> GetAllAsync() =>
                Task.FromResult<IReadOnlyList<T>>(Items.Where(x => !x.IsDeleted).ToList());

            public Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, bool ignoreQueryFilters = false)
            {
                return Task.FromResult<IReadOnlyList<T>>(ApplyFilter(filter, ignoreQueryFilters).ToList());
            }

            public Task<IEnumerable<TResult>> GetAsync<TResult>(Expression<Func<T, bool>>? filter = null, Expression<Func<T, TResult>>? selector = null)
            {
                if (selector == null)
                    throw new ArgumentException("Selector is required", nameof(selector));

                return Task.FromResult<IEnumerable<TResult>>(ApplyFilter(filter).Select(selector.Compile()).ToList());
            }

            public Task<T?> GetByIdAsync(int id) =>
                Task.FromResult(Items.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

            public IQueryable<T> GetQuery() => Items.Where(x => !x.IsDeleted).AsQueryable();

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
