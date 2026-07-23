using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;

namespace iLearn.Tests
{
    public sealed class CourseServiceVisibilityTests
    {
        private static readonly DateTime Now = new(2026, 7, 23, 9, 0, 0);

        [Fact]
        public async Task GetCourseLearnersAsync_HidesEnrollmentWithOnlySoftDeletedLink_RemoveLearnerEquivalent()
        {
            var course = new Course { Id = 1, Code = "C-001", Title = "Course 1", Status = CourseStatus.Open };
            var assignment = new Assignment { Id = 100, CourseId = 1, IsDeleted = false };
            var enrollment = new Enrollment { Id = 10, CourseId = 1, LearnerCode = "EMP001", Progress = 20 };
            var link = new EnrollmentAssignment
            {
                Id = 1000,
                EnrollmentId = enrollment.Id,
                Enrollment = enrollment,
                AssignmentId = assignment.Id,
                Assignment = assignment,
                IsDeleted = true,
                StartDate = Now.AddDays(-3),
                DueDate = Now.AddDays(5),
            };
            enrollment.AssignmentLinks.Add(link);

            var service = CreateService([course], [enrollment], [assignment], [link]);

            var rows = await service.GetCourseLearnersAsync(1);

            Assert.Empty(rows);
        }

        [Fact]
        public async Task GetCourseLearnersAsync_EnrollmentWithoutAnyLink_RemainsVisible()
        {
            var course = new Course { Id = 1, Code = "C-001", Title = "Course 1", Status = CourseStatus.Open };
            var enrollment = new Enrollment
            {
                Id = 11,
                CourseId = 1,
                LearnerCode = "EMP002",
                Progress = 15,
                StartDate = Now.AddDays(-7),
                DueDate = Now.AddDays(3),
            };

            var service = CreateService([course], [enrollment], [], []);

            var rows = await service.GetCourseLearnersAsync(1);

            var row = Assert.Single(rows);
            Assert.Equal("EMP002", row.LearnerCode);
            Assert.Equal(enrollment.StartDate, row.StartDate);
            Assert.Equal(enrollment.DueDate, row.DueDate);
        }

        [Fact]
        public async Task GetCourseLearnersAsync_MixedLinks_UsesOnlyActiveLinkDates()
        {
            var course = new Course { Id = 1, Code = "C-001", Title = "Course 1", Status = CourseStatus.Open };
            var deletedAssignment = new Assignment { Id = 101, CourseId = 1, IsDeleted = true };
            var activeAssignment = new Assignment { Id = 102, CourseId = 1, IsDeleted = false };

            var enrollment = new Enrollment
            {
                Id = 12,
                CourseId = 1,
                LearnerCode = "EMP003",
                Progress = 30,
                StartDate = Now.AddDays(-30),
                DueDate = Now.AddDays(-10),
            };

            var deletedLink = new EnrollmentAssignment
            {
                Id = 1001,
                EnrollmentId = enrollment.Id,
                Enrollment = enrollment,
                AssignmentId = deletedAssignment.Id,
                Assignment = deletedAssignment,
                IsDeleted = true,
                StartDate = Now.AddDays(-30),
                DueDate = Now.AddDays(-10),
            };

            var activeLink = new EnrollmentAssignment
            {
                Id = 1002,
                EnrollmentId = enrollment.Id,
                Enrollment = enrollment,
                AssignmentId = activeAssignment.Id,
                Assignment = activeAssignment,
                IsDeleted = false,
                StartDate = Now.AddDays(-4),
                DueDate = Now.AddDays(6),
            };

            enrollment.AssignmentLinks.Add(deletedLink);
            enrollment.AssignmentLinks.Add(activeLink);

            var service = CreateService([course], [enrollment], [deletedAssignment, activeAssignment], [deletedLink, activeLink]);

            var rows = await service.GetCourseLearnersAsync(1);

            var row = Assert.Single(rows);
            Assert.Equal(activeLink.StartDate, row.StartDate);
            Assert.Equal(activeLink.DueDate, row.DueDate);
        }

        [Fact]
        public async Task GetCourseDashboardAsync_KpiCountsOnlyVisibleEnrollments()
        {
            var course = new Course { Id = 1, Code = "C-001", Title = "Course 1", Status = CourseStatus.Open };
            var activeAssignment = new Assignment { Id = 103, CourseId = 1, AssignmentNo = "ASG-1", IsDeleted = false };
            var deletedAssignment = new Assignment { Id = 104, CourseId = 1, AssignmentNo = "ASG-2", IsDeleted = true };

            var visibleEnrollment = new Enrollment { Id = 20, CourseId = 1, LearnerCode = "EMP004", IsCompleted = true, Progress = 100 };
            var hiddenEnrollment = new Enrollment { Id = 21, CourseId = 1, LearnerCode = "EMP005", IsCompleted = true, Progress = 100 };

            var visibleLink = new EnrollmentAssignment
            {
                Id = 2001,
                EnrollmentId = visibleEnrollment.Id,
                Enrollment = visibleEnrollment,
                AssignmentId = activeAssignment.Id,
                Assignment = activeAssignment,
                IsDeleted = false,
            };
            var hiddenLink = new EnrollmentAssignment
            {
                Id = 2002,
                EnrollmentId = hiddenEnrollment.Id,
                Enrollment = hiddenEnrollment,
                AssignmentId = deletedAssignment.Id,
                Assignment = deletedAssignment,
                IsDeleted = true,
            };

            visibleEnrollment.AssignmentLinks.Add(visibleLink);
            hiddenEnrollment.AssignmentLinks.Add(hiddenLink);

            var service = CreateService(
                [course],
                [visibleEnrollment, hiddenEnrollment],
                [activeAssignment, deletedAssignment],
                [visibleLink, hiddenLink]);

            var dashboard = await service.GetCourseDashboardAsync(1);

            Assert.NotNull(dashboard);
            Assert.Equal(1, dashboard!.Kpi.LearnerCount);
            Assert.Equal(1, dashboard.Kpi.CompletedCount);
        }

        private static CourseService CreateService(
            IEnumerable<Course> courses,
            IEnumerable<Enrollment> enrollments,
            IEnumerable<Assignment> assignments,
            IEnumerable<EnrollmentAssignment> enrollmentAssignments)
        {
            var courseRepo = new InMemoryCourseRepository(courses, Now);
            var enrollmentRepo = new InMemoryGenericRepository<Enrollment>(enrollments, Now);
            var assignmentRepo = new InMemoryGenericRepository<Assignment>(assignments, Now);

            return new CourseService(
                courseRepo,
                new InMemoryGenericRepository<CourseContentItem>([], Now),
                new InMemoryGenericRepository<CourseVersion>([], Now),
                new FakeCourseAssignmentService(),
                new InMemoryGenericRepository<ContentItem>([], Now),
                new InMemoryGenericRepository<FileStorage>([], Now),
                enrollmentRepo,
                new InMemoryGenericRepository<LearningLog>([], Now),
                assignmentRepo,
                new InMemoryGenericRepository<EnrollmentAssignment>(enrollmentAssignments, Now),
                new InMemoryGenericRepository<AssignmentCourse>([], Now),
                new FakeScormService(),
                new FakeUnitOfWork(),
                new FakeLearnerApiService(),
                new FakeAdminActivityService(),
                new FakeCurrentUserService(),
                new FakeDateTime(Now),
                new FakeCourseVersionService());
        }

        private sealed class InMemoryCourseRepository : InMemoryGenericRepository<Course>, ICourseRepository
        {
            public InMemoryCourseRepository(IEnumerable<Course> items, DateTime now) : base(items, now) { }

            public Task<IEnumerable<Course>> GetActiveCoursesAsync() =>
                Task.FromResult<IEnumerable<Course>>(Items.Where(c => c.Status == CourseStatus.Open).ToList());

            public Task<bool> IsCourseCodeUniqueAsync(string code) =>
                Task.FromResult(!Items.Any(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase)));
        }

        private sealed class FakeCourseVersionService : ICourseVersionService
        {
            public Task<CreateCourseVersionDto> GetVersionByIdAsync(int versionId) => throw new NotSupportedException();
            public Task<IEnumerable<CourseVersionDto>> GetCourseVersionsAsync(int courseId) => Task.FromResult<IEnumerable<CourseVersionDto>>([]);
            public Task<CourseVersionLearnerImpactDto> GetVersionLearnerImpactAsync(int courseId) => throw new NotSupportedException();
            public Task<CourseVersionReadinessDto> GetVersionReadinessAsync(int versionId) => throw new NotSupportedException();
            public Task<CourseVersionDto> CreateVersionAsync(int courseId, CreateCourseVersionDto model, List<Microsoft.AspNetCore.Http.IFormFile> files) => throw new NotSupportedException();
            public Task<CourseVersionDto> UpdateVersionAsync(int versionId, CreateCourseVersionDto model, List<Microsoft.AspNetCore.Http.IFormFile> files) => throw new NotSupportedException();
            public Task DeleteVersionAsync(int versionId) => throw new NotSupportedException();
            public Task SetActiveVersionAsync(int courseId, int versionId, CourseVersionLearnerPolicy learnerPolicy = CourseVersionLearnerPolicy.NewLearnersOnly) => throw new NotSupportedException();
        }

        private sealed class FakeCourseAssignmentService : ICourseAssignmentService
        {
            public Task AssignGeneralCoursesToNewUserAsync(string employeeId) => throw new NotSupportedException();
            public Task AssignCourseToEmployees(int courseId, List<string> employeeCodes, DateTime? startDate, DateTime? dueDate, int? assignmentRuleId = null, bool forceReset = false) => throw new NotSupportedException();
            public Task AssignCoursesToEmployees(IReadOnlyDictionary<int, int> assignmentRuleIdsByCourseId, List<string> employeeCodes, DateTime? startDate, DateTime? dueDate, bool forceReset = false) => throw new NotSupportedException();
            public Task<List<AssignmentHistoryDto>> GetAssignmentHistoryAsync() => throw new NotSupportedException();
            public Task<AssignmentConflictDto> CheckAssignmentConflictsAsync(int courseId, List<string> employeeCodes, DateTime startDate, DateTime dueDate) => throw new NotSupportedException();
        }

        private sealed class FakeScormService : IScormService
        {
            public Task<ScormManifestDto> ExtractAndParseScormAsync(byte[] fileContent, string folderName) => throw new NotSupportedException();
            public Task<ScormManifestDto> ExtractAndParseScormFromFileAsync(string zipFilePath, string folderName) => throw new NotSupportedException();
            public Task<string> SavePackageToArchiveAsync(Stream stream, string archiveFileName) => throw new NotSupportedException();
            public void DeleteScormFolder(string folderName) => throw new NotSupportedException();
            public void DeleteArchiveFile(string storagePath) => throw new NotSupportedException();
            public string GetScormUrl(string folderName, string launchHref) => throw new NotSupportedException();
            public string GetArchiveFullPath(string relativePath) => throw new NotSupportedException();
            public (int FileCount, long TotalSize) GetFolderInfo(string folderName) => throw new NotSupportedException();
        }

        private sealed class FakeUnitOfWork : IUnitOfWork
        {
            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
            public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : BaseEntity => Task.CompletedTask;
            public void Detach<T>(T entity) where T : BaseEntity { }
            public void Dispose() { }
        }

        private sealed class FakeLearnerApiService : ILearnerApiService
        {
            public Task<ExternalLearnerDto> GetLearnerByCodeAsync(string Code) => throw new NotSupportedException();
            public Task<AllLearnersApiResponse> GetLearnerAsync() => throw new NotSupportedException();
            public Task<DivisionApiResponse> GetLearnersByDivisionsAsync(string[] divisions, int skip = 0, int take = 20) => throw new NotSupportedException();
            public Task<string> GetLearnersDxGridAsync(string queryString) => throw new NotSupportedException();
            public Task<object> GetSectionsAsync(string queryString) => throw new NotSupportedException();
            public Task<object> GetDivisionsAsync(string queryString) => throw new NotSupportedException();
            public Task<object> GetDepartmentsAsync(string queryString) => throw new NotSupportedException();
            public Task<object> GetPositionsAsync(string queryString) => throw new NotSupportedException();
            public Task<Dictionary<string, ExternalLearnerDto>> GetLearnersByCodesAsync(IEnumerable<string> codes)
            {
                var map = codes.Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(
                    code => code,
                    code => new ExternalLearnerDto { Code = code, Name = code },
                    StringComparer.OrdinalIgnoreCase);
                return Task.FromResult(map);
            }
            public Task<Dictionary<string, EmployeeCsvDto>> GetEmployeesByNidsAsync(IEnumerable<string> nids) => throw new NotSupportedException();
        }

        private sealed class FakeAdminActivityService : IAdminActivityService
        {
            public Task LogAsync(string actionType, string entityType, int? entityId, string title, string? description = null, int? divisionId = null, string? dataJson = null) => Task.CompletedTask;
            public Task<IReadOnlyList<AdminActivityDto>> GetRecentActivitiesAsync(int take = 20, int? divisionId = null) => Task.FromResult<IReadOnlyList<AdminActivityDto>>([]);
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public string UserId => "tester";
            public string FullName => "tester";
            public bool IsAuthenticated => true;
            public int? DivisionId => null;
            public string? DivisionName => "ALL";
            public bool IsSuperAdmin => true;
        }

        private sealed class FakeDateTime : IDateTime
        {
            public FakeDateTime(DateTime now) { Now = now; }
            public DateTime Now { get; }
            public CultureInfo CultureInfo => CultureInfo.InvariantCulture;
            public DateTime UnixTime => Now;
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

            public Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, bool ignoreQueryFilters = false) =>
                Task.FromResult<IReadOnlyList<T>>(ApplyFilter(filter, ignoreQueryFilters).ToList());

            public Task<IEnumerable<TResult>> GetAsync<TResult>(Expression<Func<T, bool>>? filter = null, Expression<Func<T, TResult>>? selector = null)
            {
                if (selector == null)
                {
                    throw new ArgumentException("Selector is required", nameof(selector));
                }

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
