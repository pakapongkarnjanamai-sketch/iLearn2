using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using Microsoft.EntityFrameworkCore.Query;
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
                Status = CourseStatus.Open,
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
            Assert.Equal("490222", enrollment.LearnerCode);
            Assert.Equal(10, enrollment.CourseId);
            Assert.Equal(100, enrollment.EnrolledCourseVersion);

            var link = service.EnrollmentAssignmentRepository.Items.Single();
            Assert.Equal(9001, link.AssignmentId);
            Assert.Same(enrollment, link.Enrollment);
            Assert.Equal(Now, link.StartDate);
            Assert.Equal(Now.AddDays(7), link.DueDate);
        }

        [Fact]
        public async Task AssignCoursesToEmployees_ClosedCourse_DoesNotCreateEnrollment()
        {
            var course = new Course
            {
                Id = 11,
                IsActive = false,
                Status = CourseStatus.Closed,
                Code = "C-11",
                Title = "Closed Course"
            };
            var version = new CourseVersion
            {
                Id = 110,
                CourseId = 11,
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
                new Dictionary<int, int> { [11] = 9002 },
                ["490222"],
                Now,
                Now.AddDays(7),
                forceReset: false);

            Assert.Empty(service.EnrollmentRepository.Items);
            Assert.Empty(service.EnrollmentAssignmentRepository.Items);
        }

        [Fact]
        public async Task AssignCoursesToEmployees_ReassignCompletedEnrollment_ResetsEnrollmentAndSnapshotsExistingLinks()
        {
            var course = new Course
            {
                Id = 20,
                IsActive = true,
                Status = CourseStatus.Open,
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
                LearnerCode = "490222",
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

        [Fact]
        public async Task AssignmentService_UpdateDescriptionAsync_UpdatesDescriptionAcrossAllBatchRules()
        {
            var rule1 = new Assignment
            {
                Id = 1,
                AssignmentNo = "AS-20260723-001",
                Description = "Old Description",
                DivisionId = 5,
            };
            var rule2 = new Assignment
            {
                Id = 2,
                AssignmentNo = "AS-20260723-001",
                Description = "Old Description",
                DivisionId = 5,
            };

            var assignmentRepo = new InMemoryGenericRepository<Assignment>([rule1, rule2], Now);
            var batchService = new AssignmentBatchService(assignmentRepo, new FakeCurrentUserService { DivisionId = 5 });
            var service = new AssignmentService(
                assignmentRepo,
                new InMemoryGenericRepository<EnrollmentAssignment>([], Now),
                new InMemoryGenericRepository<Enrollment>([], Now),
                new InMemoryGenericRepository<Course>([], Now),
                null!,
                batchService,
                null!,
                new InMemoryGenericRepository<LearnerGroupMember>([], Now),
                null!,
                new FakeDateTime(Now),
                new FakeUnitOfWork()
            );

            var result = await service.UpdateDescriptionAsync(
                1,
                new UpdateAssignmentDescriptionDto { Description = "  New Updated Description  " },
                divisionId: 5);

            Assert.True(result.Success);
            Assert.Equal("Description updated successfully.", result.Message);
            Assert.Equal("New Updated Description", rule1.Description);
            Assert.Equal("New Updated Description", rule2.Description);
        }

        [Fact]
        public async Task AssignmentService_GetDashboardAsync_LimitsLearnerGroupsToAssignmentTargetGroups()
        {
            var targetGroup = new LearnerGroup { Id = 10, Name = "Target Group", DivisionId = 5 };
            var unrelatedGroup = new LearnerGroup { Id = 20, Name = "Unrelated Group", DivisionId = 5 };
            var course = new Course { Id = 30, Code = "C-30", Title = "Course 30" };
            var enrollment = new Enrollment { Id = 40, LearnerCode = "490222", CourseId = course.Id, Course = course };
            var assignment = new Assignment
            {
                Id = 50,
                AssignmentNo = "AS-GROUP-001",
                CourseId = course.Id,
                LearnerGroupId = targetGroup.Id,
                LearnerGroup = targetGroup,
                DivisionId = 5,
            };

            var service = CreateAssignmentService(
                assignments: [assignment],
                enrollmentAssignments: [new EnrollmentAssignment { Id = 60, AssignmentId = assignment.Id, EnrollmentId = enrollment.Id, Enrollment = enrollment }],
                enrollments: [enrollment],
                courses: [course],
                learnerGroupMembers:
                [
                    new LearnerGroupMember { Id = 70, LearnerCode = enrollment.LearnerCode, LearnerGroupId = unrelatedGroup.Id, LearnerGroup = unrelatedGroup },
                ]);

            var dashboard = await service.GetDashboardAsync(assignment.Id, divisionId: 5);

            var learner = Assert.Single(dashboard!.Learners);
            Assert.Equal(["Target Group"], learner.LearnerGroups);
        }

        [Fact]
        public async Task AssignmentService_GetDashboardAsync_DoesNotReportCurrentMembershipForDirectAssignments()
        {
            var currentMembership = new LearnerGroup { Id = 20, Name = "Current Membership", DivisionId = 5 };
            var course = new Course { Id = 30, Code = "C-30", Title = "Course 30" };
            var enrollment = new Enrollment { Id = 40, LearnerCode = "490222", CourseId = course.Id, Course = course };
            var assignment = new Assignment
            {
                Id = 50,
                AssignmentNo = "AS-DIRECT-001",
                CourseId = course.Id,
                DivisionId = 5,
            };

            var service = CreateAssignmentService(
                assignments: [assignment],
                enrollmentAssignments: [new EnrollmentAssignment { Id = 60, AssignmentId = assignment.Id, EnrollmentId = enrollment.Id, Enrollment = enrollment }],
                enrollments: [enrollment],
                courses: [course],
                learnerGroupMembers:
                [
                    new LearnerGroupMember { Id = 70, LearnerCode = enrollment.LearnerCode, LearnerGroupId = currentMembership.Id, LearnerGroup = currentMembership },
                ]);

            var dashboard = await service.GetDashboardAsync(assignment.Id, divisionId: 5);

            var learner = Assert.Single(dashboard!.Learners);
            Assert.Empty(learner.LearnerGroups);
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
                new FakeScormRuntimeStateService(),
                new FakeDateTime(Now),
                unitOfWork);

            return new CourseAssignmentServiceHarness(service, enrollmentRepo, enrollmentAssignmentRepo);
        }

        private static AssignmentService CreateAssignmentService(
            IEnumerable<Assignment> assignments,
            IEnumerable<EnrollmentAssignment> enrollmentAssignments,
            IEnumerable<Enrollment> enrollments,
            IEnumerable<Course> courses,
            IEnumerable<LearnerGroupMember> learnerGroupMembers)
        {
            var assignmentRepo = new InMemoryGenericRepository<Assignment>(assignments, Now);
            var batchService = new AssignmentBatchService(assignmentRepo, new FakeCurrentUserService { DivisionId = 5 });

            return new AssignmentService(
                assignmentRepo,
                new InMemoryGenericRepository<EnrollmentAssignment>(enrollmentAssignments, Now),
                new InMemoryGenericRepository<Enrollment>(enrollments, Now),
                new InMemoryGenericRepository<Course>(courses, Now),
                new FakeLearnerApiService(),
                batchService,
                null!,
                new InMemoryGenericRepository<LearnerGroupMember>(learnerGroupMembers, Now),
                null!,
                new FakeDateTime(Now),
                new FakeUnitOfWork());
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

        private sealed class FakeScormRuntimeStateService : IScormRuntimeStateService
        {
            public Task<IReadOnlyList<ScormRuntimeStateDto>> GetActiveStatesAsync(int enrollmentId, DateTime? resetAt = null) =>
                Task.FromResult<IReadOnlyList<ScormRuntimeStateDto>>([]);

            public Task<int> ClearForEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken = default) => Task.FromResult(0);

            public Task<int> ClearForEnrollmentsAsync(IReadOnlyCollection<int> enrollmentIds, bool saveChanges = true, CancellationToken cancellationToken = default) => Task.FromResult(0);

            public Task<IReadOnlyList<ScormRuntimeStateDto>> UpsertAsync(int enrollmentId, IReadOnlyCollection<ScormRuntimeContentItemCommitDto> contentItems, CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyList<ScormRuntimeStateDto>>([]);
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
                return Task.FromResult(codes
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        code => code,
                        code => new ExternalLearnerDto { Code = code, Name = code },
                        StringComparer.OrdinalIgnoreCase));
            }

            public Task<Dictionary<string, EmployeeCsvDto>> GetEmployeesByNidsAsync(IEnumerable<string> nids)
            {
                return Task.FromResult(new Dictionary<string, EmployeeCsvDto>(StringComparer.OrdinalIgnoreCase));
            }
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
                => Task.FromResult<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>(new FakeDbContextTransaction());

            public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
                where T : iLearn.Domain.Common.BaseEntity
                => Task.CompletedTask;

            public void Detach<T>(T entity) where T : iLearn.Domain.Common.BaseEntity
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class FakeDbContextTransaction : Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction
        {
            public Guid TransactionId => Guid.NewGuid();
            public void Commit() { }
            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void Rollback() { }
            public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        private sealed class InMemoryCourseRepository : InMemoryGenericRepository<Course>, ICourseRepository
        {
            public InMemoryCourseRepository(IEnumerable<Course> items, DateTime now)
                : base(items, now)
            {
            }

            public Task<IEnumerable<Course>> GetActiveCoursesAsync()
            {
                return Task.FromResult<IEnumerable<Course>>(Items.Where(c => c.Status == CourseStatus.Open).ToList());
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
                return new TestAsyncEnumerable<T>(Items.Where(x => !x.IsDeleted).AsQueryable());
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
            public IQueryProvider Provider { get; }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            }
        }

        private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider, IQueryProvider
        {
            private readonly IQueryProvider _inner;

            public TestAsyncQueryProvider(IQueryProvider inner)
            {
                _inner = inner;
            }

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

            public TestAsyncEnumerator(IEnumerator<T> inner)
            {
                _inner = inner;
            }

            public T Current => _inner.Current;

            public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());

            public ValueTask DisposeAsync()
            {
                _inner.Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
