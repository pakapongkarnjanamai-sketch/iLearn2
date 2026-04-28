using iLearn.API.Controllers;
using iLearn.API.Services;
using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace iLearn.Tests
{
    public sealed class LearningLogsRuntimeValidationTests
    {
        [Fact]
        public async Task CommitRuntime_RejectsOversizedSuspendData()
        {
            var controller = CreateController();

            var result = await controller.CommitRuntime(new ScormRuntimeCommitRequestDto
            {
                EnrollmentId = 10,
                Resources =
                [
                    new ScormRuntimeResourceCommitDto
                    {
                        ResourceId = 100,
                        ScormVersion = ScormRuntimeFieldMap.Scorm2004,
                        SuspendData = new string('x', ScormRuntimeLimits.SuspendDataMaxLength + 1)
                    }
                ]
            });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);
            Assert.False(response.Success);
            Assert.Contains("Suspend data exceeds the supported limit", response.Message);
        }

        [Fact]
        public void LearnerProxyIdentityResolver_AcceptsValidSignedHeaders()
        {
            const string sharedSecret = "runtime-secret";
            const string studentCode = "490222";
            var timestamp = LearnerProxyAuthSignature.CreateTimestamp(DateTimeOffset.UtcNow);
            var resolver = new LearnerProxyIdentityResolver(
                Options.Create(new LearnerProxyAuthOptions
                {
                    SharedSecret = sharedSecret,
                    TimestampToleranceSeconds = 300
                }),
                NullLogger<LearnerProxyIdentityResolver>.Instance);

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.Path = "/api/LearningLogs/commit-runtime";

            context.Request.Headers[LearnerProxyAuthHeaders.StudentCode] = studentCode;
            context.Request.Headers[LearnerProxyAuthHeaders.Timestamp] = timestamp;
            context.Request.Headers[LearnerProxyAuthHeaders.Signature] = LearnerProxyAuthSignature.Compute(
                sharedSecret,
                studentCode,
                timestamp,
                HttpMethods.Post,
                "/api/LearningLogs/commit-runtime");

            var accepted = resolver.TryResolveStudentCode(context, out var resolvedStudentCode, out var statusCode, out var errorMessage);

            Assert.True(accepted);
            Assert.Equal(studentCode, resolvedStudentCode);
            Assert.Equal(StatusCodes.Status200OK, statusCode);
            Assert.Equal(string.Empty, errorMessage);
        }

        [Fact]
        public void LearnerProxyIdentityResolver_RejectsInvalidSignature()
        {
            var resolver = new LearnerProxyIdentityResolver(
                Options.Create(new LearnerProxyAuthOptions
                {
                    SharedSecret = "runtime-secret",
                    TimestampToleranceSeconds = 300
                }),
                NullLogger<LearnerProxyIdentityResolver>.Instance);

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.Path = "/api/LearningLogs/commit-runtime";
            context.Request.Headers[LearnerProxyAuthHeaders.StudentCode] = "490222";
            context.Request.Headers[LearnerProxyAuthHeaders.Timestamp] = LearnerProxyAuthSignature.CreateTimestamp(DateTimeOffset.UtcNow);
            context.Request.Headers[LearnerProxyAuthHeaders.Signature] = "BAD-SIGNATURE";

            var accepted = resolver.TryResolveStudentCode(context, out var resolvedStudentCode, out var statusCode, out var errorMessage);

            Assert.False(accepted);
            Assert.Equal(string.Empty, resolvedStudentCode);
            Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
            Assert.Equal("Invalid learner proxy signature.", errorMessage);
        }

        private static LearningLogsController CreateController()
        {
            var enrollmentRepo = new InMemoryGenericRepository<Enrollment>(
            [
                new Enrollment
                {
                    Id = 10,
                    StudentCode = "490222",
                    EnrolledCourseVersion = 20,
                    IsCompleted = false
                }
            ]);

            var versionRepo = new InMemoryGenericRepository<CourseVersion>(
            [
                new CourseVersion
                {
                    Id = 20,
                    CourseId = 1,
                    CourseResources =
                    [
                        new CourseResource { Id = 1, ResourceId = 100 },
                        new CourseResource { Id = 2, ResourceId = 101 }
                    ]
                }
            ]);

            var controller = new LearningLogsController(
                new InMemoryGenericRepository<LearningLog>([]),
                enrollmentRepo,
                versionRepo,
                new InMemoryGenericRepository<EnrollmentAssignment>([]),
                new FakeCurrentUserService(),
                new MemoryCache(new MemoryCacheOptions()),
                new FakeLearnerProxyIdentityResolver(),
                new FakeScormRuntimeStateService());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
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

        private sealed class FakeLearnerProxyIdentityResolver : ILearnerProxyIdentityResolver
        {
            public bool TryResolveStudentCode(HttpContext context, out string studentCode, out int statusCode, out string errorMessage)
            {
                studentCode = "490222";
                statusCode = StatusCodes.Status200OK;
                errorMessage = string.Empty;
                return true;
            }
        }

        private sealed class FakeScormRuntimeStateService : IScormRuntimeStateService
        {
            public Task<IReadOnlyList<ScormRuntimeStateDto>> GetActiveStatesAsync(int enrollmentId, DateTime? resetAt = null)
            {
                return Task.FromResult<IReadOnlyList<ScormRuntimeStateDto>>([]);
            }

            public Task<IReadOnlyList<ScormRuntimeStateDto>> UpsertAsync(int enrollmentId, IReadOnlyCollection<ScormRuntimeResourceCommitDto> resources, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<ScormRuntimeStateDto>>(
                    resources.Select(resource => new ScormRuntimeStateDto
                    {
                        EnrollmentId = enrollmentId,
                        ResourceId = resource.ResourceId,
                        ScormVersion = resource.ScormVersion
                    }).ToList());
            }
        }

        private sealed class InMemoryGenericRepository<T> : IGenericRepository<T> where T : BaseEntity
        {
            public InMemoryGenericRepository(IEnumerable<T> items)
            {
                Items = items.ToList();
            }

            public List<T> Items { get; }

            public Task<IReadOnlyList<T>> GetAllAsync() => Task.FromResult<IReadOnlyList<T>>(Items.Where(item => !item.IsDeleted).ToList());

            public Task<T?> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(item => item.Id == id && !item.IsDeleted));

            public Task<T> AddAsync(T entity)
            {
                if (!Items.Contains(entity))
                {
                    Items.Add(entity);
                }

                return Task.FromResult(entity);
            }

            public Task<T> AddWithoutSaveAsync(T entity) => AddAsync(entity);

            public Task UpdateAsync(T entity)
            {
                UpdateWithoutSave(entity);
                return Task.CompletedTask;
            }

            public void UpdateWithoutSave(T entity)
            {
                if (!Items.Contains(entity))
                {
                    Items.Add(entity);
                }
            }

            public Task DeleteAsync(T entity)
            {
                DeleteWithoutSave(entity);
                return Task.CompletedTask;
            }

            public void DeleteWithoutSave(T entity)
            {
                entity.IsDeleted = true;
            }

            public Task HardDeleteAsync(T entity)
            {
                Items.Remove(entity);
                return Task.CompletedTask;
            }

            public IQueryable<T> GetQuery() => Items.Where(item => !item.IsDeleted).AsQueryable();

            public Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, bool ignoreQueryFilters = false)
            {
                var query = ignoreQueryFilters ? Items.AsEnumerable() : Items.Where(item => !item.IsDeleted);
                var result = filter == null ? query.ToList() : query.Where(filter.Compile()).ToList();
                return Task.FromResult<IReadOnlyList<T>>(result);
            }

            public Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
            {
                var query = Items.Where(item => !item.IsDeleted);
                return Task.FromResult(filter == null ? query.Count() : query.Count(filter.Compile()));
            }

            public Task<IEnumerable<TResult>> GetAsync<TResult>(Expression<Func<T, bool>>? filter = null, Expression<Func<T, TResult>>? selector = null)
            {
                if (selector == null)
                {
                    throw new ArgumentException("Selector is required", nameof(selector));
                }

                var query = Items.Where(item => !item.IsDeleted);
                if (filter != null)
                {
                    query = query.Where(filter.Compile()).ToList();
                }

                return Task.FromResult<IEnumerable<TResult>>(query.Select(selector.Compile()).ToList());
            }
        }
    }
}