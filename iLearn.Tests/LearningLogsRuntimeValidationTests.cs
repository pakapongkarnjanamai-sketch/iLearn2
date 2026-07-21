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
                ContentItems =
                [
                    new ScormRuntimeContentItemCommitDto
                    {
                        ContentItemId = 100,
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
        public async Task CommitRuntime_DoesNotCompleteEnrollment_WhenScorm2004SuccessFailedButCompletionCompleted()
        {
            var controller = CreateController(out var logRepo, out var enrollmentRepo);

            var result = await controller.CommitRuntime(new ScormRuntimeCommitRequestDto
            {
                EnrollmentId = 10,
                ContentItems =
                [
                    new ScormRuntimeContentItemCommitDto
                    {
                        ContentItemId = 100,
                        ScormVersion = ScormRuntimeFieldMap.Scorm2004,
                        CompletionStatus = "completed",
                        SuccessStatus = "failed",
                        RawScore = 2,
                        SessionTime = "00:00:18"
                    },
                    new ScormRuntimeContentItemCommitDto
                    {
                        ContentItemId = 101,
                        ScormVersion = ScormRuntimeFieldMap.Scorm12,
                        LessonStatus = "passed",
                        RawScore = 100,
                        SessionTime = "00:00:10"
                    }
                ]
            });

            Assert.IsType<OkObjectResult>(result);
            var failedExamLog = Assert.Single(logRepo.Items, log => log.ContentItemId == 100);
            Assert.Equal("failed", failedExamLog.Status);
            Assert.Equal(0, failedExamLog.Progress);

            var enrollment = Assert.Single(enrollmentRepo.Items);
            Assert.False(enrollment.IsCompleted);
            Assert.Equal(50, enrollment.Progress);
        }

        [Fact]
        public async Task CommitRuntime_DoesNotCompleteExam_WhenCompletionCompletedButSuccessUnknown()
        {
            var controller = CreateController(out var logRepo, out var enrollmentRepo);

            var result = await controller.CommitRuntime(new ScormRuntimeCommitRequestDto
            {
                EnrollmentId = 10,
                ContentItems =
                [
                    new ScormRuntimeContentItemCommitDto
                    {
                        ContentItemId = 100,
                        ScormVersion = ScormRuntimeFieldMap.Scorm2004,
                        CompletionStatus = "completed",
                        SuccessStatus = "unknown",
                        RawScore = 80,
                        SessionTime = "00:00:11"
                    }
                ]
            });

            Assert.IsType<OkObjectResult>(result);
            var examLog = Assert.Single(logRepo.Items, log => log.ContentItemId == 100);
            Assert.Equal("incomplete", examLog.Status);
            Assert.Equal(0, examLog.Progress);
            Assert.Equal(80, examLog.Score);

            var enrollment = Assert.Single(enrollmentRepo.Items);
            Assert.False(enrollment.IsCompleted);
            Assert.Equal(0, enrollment.Progress);
        }

        [Fact]
        public async Task CommitRuntime_DoesNotCompleteScorm12ContentItem_WhenLessonIncompleteButCompletionAliasIsStaleCompleted()
        {
            var controller = CreateController(out var logRepo, out var enrollmentRepo);

            var result = await controller.CommitRuntime(new ScormRuntimeCommitRequestDto
            {
                EnrollmentId = 10,
                ContentItems =
                [
                    new ScormRuntimeContentItemCommitDto
                    {
                        ContentItemId = 100,
                        ScormVersion = ScormRuntimeFieldMap.Scorm12,
                        LessonStatus = "incomplete",
                        CompletionStatus = "completed",
                        SuccessStatus = "unknown",
                        RawScore = 20,
                        SuspendData = "N4IgDiBcCMA0IFsoCZ4DcoG0AMBdAvkA",
                        SessionTime = "00:00:00"
                    }
                ]
            });

            Assert.IsType<OkObjectResult>(result);
            var learnLog = Assert.Single(logRepo.Items, log => log.ContentItemId == 100);
            Assert.Equal("incomplete", learnLog.Status);
            Assert.Equal(0, learnLog.Progress);
            Assert.Equal(20, learnLog.Score);

            var enrollment = Assert.Single(enrollmentRepo.Items);
            Assert.False(enrollment.IsCompleted);
            Assert.Equal(0, enrollment.Progress);
        }

        [Fact]
        public async Task CommitRuntime_CreatesNewActiveLogAfterResetAt()
        {
            var controller = CreateController(out var logRepo, out var enrollmentRepo);
            var resetAt = new DateTime(2026, 4, 28, 11, 50, 0, DateTimeKind.Local);
            var oldLogCreatedAt = resetAt.AddMinutes(-5);
            var enrollment = Assert.Single(enrollmentRepo.Items);
            enrollment.ResetAt = resetAt;

            logRepo.Items.Add(new LearningLog
            {
                Id = 1,
                EnrollmentId = 10,
                LearnerCode = "490222",
                CourseVersionId = 20,
                ContentItemId = 100,
                Status = "incomplete",
                Progress = 0,
                CreatedAt = oldLogCreatedAt
            });

            var result = await controller.CommitRuntime(new ScormRuntimeCommitRequestDto
            {
                EnrollmentId = 10,
                ContentItems =
                [
                    new ScormRuntimeContentItemCommitDto
                    {
                        ContentItemId = 100,
                        ScormVersion = ScormRuntimeFieldMap.Scorm12,
                        LessonStatus = "passed",
                        RawScore = 100,
                        SessionTime = "00:01:00"
                    }
                ]
            });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(2, logRepo.Items.Count);
            Assert.Contains(logRepo.Items, log => log.Id == 1 && log.Status == "incomplete" && log.CreatedAt == oldLogCreatedAt);
            Assert.Contains(logRepo.Items, log => log.Id != 1 && log.ContentItemId == 100 && log.Status == "passed" && log.CreatedAt >= resetAt);
            Assert.False(enrollment.IsCompleted);
            Assert.Equal(50, enrollment.Progress);
        }

        [Fact]
        public async Task CommitRuntime_AllowsFinalSessionSync_WhenEnrollmentAlreadyCompleted()
        {
            var controller = CreateController(out var logRepo, out var enrollmentRepo);
            var enrollment = Assert.Single(enrollmentRepo.Items);
            enrollment.IsCompleted = true;
            enrollment.CompletedDate = new DateTime(2026, 4, 28, 11, 59, 0, DateTimeKind.Local);

            var result = await controller.CommitRuntime(new ScormRuntimeCommitRequestDto
            {
                EnrollmentId = 10,
                ContentItems =
                [
                    new ScormRuntimeContentItemCommitDto
                    {
                        ContentItemId = 100,
                        ScormVersion = ScormRuntimeFieldMap.Scorm12,
                        LessonStatus = "passed",
                        RawScore = 100,
                        SessionTime = "00:00:09"
                    }
                ]
            });

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IReadOnlyList<ScormRuntimeStateDto>>>(ok.Value);
            Assert.True(response.Success);
            Assert.Equal("Runtime committed.", response.Message);

            var learnLog = Assert.Single(logRepo.Items, log => log.ContentItemId == 100);
            Assert.Equal("passed", learnLog.Status);
            Assert.Equal(100, learnLog.Score);
            Assert.Equal("00:00:09", learnLog.SessionTime);
            Assert.Equal(9, learnLog.TotalSecondsPlayed);
        }

        [Fact]
        public async Task ResetProgress_ClearsEnrollmentSummaryAndAssignmentSnapshot()
        {
            var controller = CreateController(out _, out var enrollmentRepo, out var assignmentRepo, out var runtimeStateService);
            var enrollment = Assert.Single(enrollmentRepo.Items);
            enrollment.IsCompleted = true;
            enrollment.CompletedDate = DateTime.Now.AddDays(-1);
            enrollment.Progress = 100;
            enrollment.TotalScore = 95;
            enrollment.TotalTimeSpent = 3600;

            assignmentRepo.Items.Add(new EnrollmentAssignment
            {
                Id = 1,
                EnrollmentId = enrollment.Id,
                AssignmentId = 9,
                SnapshotCompleted = true,
                SnapshotCompletedDate = DateTime.Now.AddDays(-1),
                SnapshotProgress = 100
            });

            var result = await controller.ResetProgress(new ResetProgressDto { EnrollmentId = enrollment.Id });

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(ok.Value);
            Assert.True(response.Success);
            Assert.False(enrollment.IsCompleted);
            Assert.Null(enrollment.CompletedDate);
            Assert.Equal(0, enrollment.Progress);
            Assert.Equal(0, enrollment.TotalScore);
            Assert.Equal(0, enrollment.TotalTimeSpent);
            Assert.NotNull(enrollment.ResetAt);

            var link = Assert.Single(assignmentRepo.Items);
            Assert.False(link.SnapshotCompleted);
            Assert.Null(link.SnapshotCompletedDate);
            Assert.Equal(0, link.SnapshotProgress);
            Assert.Equal(enrollment.Id, runtimeStateService.ClearedEnrollmentId);
        }

        [Fact]
        public async Task CommitRuntime_OnlyAddsNewSessionTimeSincePreviousCommit()
        {
            var controller = CreateController(out var logRepo, out _);

            foreach (var sessionTime in new[] { "00:00:06", "00:00:08", "00:00:10" })
            {
                var result = await controller.CommitRuntime(new ScormRuntimeCommitRequestDto
                {
                    EnrollmentId = 10,
                    ContentItems =
                    [
                        new ScormRuntimeContentItemCommitDto
                        {
                            ContentItemId = 100,
                            ScormVersion = ScormRuntimeFieldMap.Scorm12,
                            LessonStatus = "incomplete",
                            SessionTime = sessionTime
                        }
                    ]
                });

                Assert.IsType<OkObjectResult>(result);
            }

            Assert.Equal(10, Assert.Single(logRepo.Items).TotalSecondsPlayed);
        }

        [Fact]
        public async Task CommitRuntime_AddsFullSessionTimeWhenNewSessionCounterResets()
        {
            var controller = CreateController(out var logRepo, out _);

            foreach (var sessionTime in new[] { "00:00:10", "00:00:05" })
            {
                var result = await controller.CommitRuntime(new ScormRuntimeCommitRequestDto
                {
                    EnrollmentId = 10,
                    ContentItems =
                    [
                        new ScormRuntimeContentItemCommitDto
                        {
                            ContentItemId = 100,
                            ScormVersion = ScormRuntimeFieldMap.Scorm12,
                            LessonStatus = "incomplete",
                            SessionTime = sessionTime
                        }
                    ]
                });

                Assert.IsType<OkObjectResult>(result);
            }

            Assert.Equal(15, Assert.Single(logRepo.Items).TotalSecondsPlayed);
        }

        [Fact]
        public async Task CommitRuntime_DoesNotDowngradePassedLogWhenIncomingCommitIsPlaceholder()
        {
            var controller = CreateController(out var logRepo, out var enrollmentRepo);
            logRepo.Items.Add(new LearningLog
            {
                Id = 1,
                EnrollmentId = 10,
                LearnerCode = "490222",
                CourseVersionId = 20,
                ContentItemId = 100,
                Status = "passed",
                Progress = 100,
                Score = 100,
                SessionTime = "00:00:10",
                TotalSecondsPlayed = 10,
                CreatedAt = new DateTime(2026, 4, 28, 11, 55, 0, DateTimeKind.Local)
            });

            var result = await controller.CommitRuntime(new ScormRuntimeCommitRequestDto
            {
                EnrollmentId = 10,
                ContentItems =
                [
                    new ScormRuntimeContentItemCommitDto
                    {
                        ContentItemId = 100,
                        ScormVersion = ScormRuntimeFieldMap.Scorm12,
                        LessonStatus = "incomplete",
                        RawScore = 0,
                        SessionTime = "00:00:20"
                    }
                ]
            });

            Assert.IsType<OkObjectResult>(result);
            var log = Assert.Single(logRepo.Items);
            Assert.Equal("passed", log.Status);
            Assert.Equal(100, log.Progress);
            Assert.Equal(100, log.Score);
            Assert.Equal("00:00:20", log.SessionTime);
            Assert.Equal(20, log.TotalSecondsPlayed);

            var enrollment = Assert.Single(enrollmentRepo.Items);
            Assert.False(enrollment.IsCompleted);
            Assert.Null(enrollment.CompletedDate);
            Assert.Equal(50, enrollment.Progress);
        }

        [Fact]
        public async Task CommitRuntime_ClearsCompletedFlagWhenRollupFallsBelowCompleteAndRestoresWhenCompleteAgain()
        {
            var controller = CreateController(out var logRepo, out var enrollmentRepo);
            var enrollment = Assert.Single(enrollmentRepo.Items);
            enrollment.IsCompleted = true;
            enrollment.CompletedDate = new DateTime(2026, 4, 28, 11, 58, 0, DateTimeKind.Local);
            enrollment.Progress = 100;

            logRepo.Items.Add(new LearningLog
            {
                Id = 1,
                EnrollmentId = 10,
                LearnerCode = "490222",
                CourseVersionId = 20,
                ContentItemId = 100,
                Status = "passed",
                Progress = 100,
                Score = 100,
                CreatedAt = new DateTime(2026, 4, 28, 11, 55, 0, DateTimeKind.Local)
            });

            var partialResult = await controller.CommitRuntime(new ScormRuntimeCommitRequestDto
            {
                EnrollmentId = 10,
                ContentItems =
                [
                    new ScormRuntimeContentItemCommitDto
                    {
                        ContentItemId = 101,
                        ScormVersion = ScormRuntimeFieldMap.Scorm12,
                        LessonStatus = "incomplete",
                        RawScore = 0,
                        SessionTime = "00:00:05"
                    }
                ]
            });

            Assert.IsType<OkObjectResult>(partialResult);
            Assert.False(enrollment.IsCompleted);
            Assert.Null(enrollment.CompletedDate);
            Assert.Equal(50, enrollment.Progress);

            var completeResult = await controller.CommitRuntime(new ScormRuntimeCommitRequestDto
            {
                EnrollmentId = 10,
                ContentItems =
                [
                    new ScormRuntimeContentItemCommitDto
                    {
                        ContentItemId = 101,
                        ScormVersion = ScormRuntimeFieldMap.Scorm12,
                        LessonStatus = "passed",
                        RawScore = 100,
                        SessionTime = "00:00:10"
                    }
                ]
            });

            Assert.IsType<OkObjectResult>(completeResult);
            Assert.True(enrollment.IsCompleted);
            Assert.Equal(100, enrollment.Progress);
            Assert.Equal(new DateTime(2026, 4, 28, 12, 0, 0, DateTimeKind.Local), enrollment.CompletedDate);
        }

        [Fact]
        public async Task CommitRuntime_IgnoresSessionTimeDeltaOverFourHours()
        {
            var controller = CreateController(out var logRepo, out _);

            foreach (var sessionTime in new[] { "00:00:10", "05:00:10" })
            {
                var result = await controller.CommitRuntime(new ScormRuntimeCommitRequestDto
                {
                    EnrollmentId = 10,
                    ContentItems =
                    [
                        new ScormRuntimeContentItemCommitDto
                        {
                            ContentItemId = 100,
                            ScormVersion = ScormRuntimeFieldMap.Scorm12,
                            LessonStatus = "incomplete",
                            SessionTime = sessionTime
                        }
                    ]
                });

                Assert.IsType<OkObjectResult>(result);
            }

            var log = Assert.Single(logRepo.Items);
            Assert.Equal("05:00:10", log.SessionTime);
            Assert.Equal(10, log.TotalSecondsPlayed);
        }

        [Fact]
        public void LearnerProxyIdentityResolver_AcceptsValidSignedHeaders()
        {
            const string sharedSecret = "runtime-secret";
            const string learnerCode = "490222";
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

            context.Request.Headers[LearnerProxyAuthHeaders.LearnerCode] = learnerCode;
            context.Request.Headers[LearnerProxyAuthHeaders.Timestamp] = timestamp;
            context.Request.Headers[LearnerProxyAuthHeaders.Signature] = LearnerProxyAuthSignature.Compute(
                sharedSecret,
                learnerCode,
                timestamp,
                HttpMethods.Post,
                "/api/LearningLogs/commit-runtime");

            var accepted = resolver.TryResolveLearnerCode(context, out var resolvedLearnerCode, out var statusCode, out var errorMessage);

            Assert.True(accepted);
            Assert.Equal(learnerCode, resolvedLearnerCode);
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
            context.Request.Headers[LearnerProxyAuthHeaders.LearnerCode] = "490222";
            context.Request.Headers[LearnerProxyAuthHeaders.Timestamp] = LearnerProxyAuthSignature.CreateTimestamp(DateTimeOffset.UtcNow);
            context.Request.Headers[LearnerProxyAuthHeaders.Signature] = "BAD-SIGNATURE";

            var accepted = resolver.TryResolveLearnerCode(context, out var resolvedLearnerCode, out var statusCode, out var errorMessage);

            Assert.False(accepted);
            Assert.Equal(string.Empty, resolvedLearnerCode);
            Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
            Assert.Equal("Invalid learner proxy signature.", errorMessage);
        }

        private static LearningLogsController CreateController()
        {
            return CreateController(out _, out _);
        }

        private static LearningLogsController CreateController(
            out InMemoryGenericRepository<LearningLog> logRepo,
            out InMemoryGenericRepository<Enrollment> enrollmentRepo)
        {
            return CreateController(out logRepo, out enrollmentRepo, out _);
        }

        private static LearningLogsController CreateController(
            out InMemoryGenericRepository<LearningLog> logRepo,
            out InMemoryGenericRepository<Enrollment> enrollmentRepo,
            out InMemoryGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo)
        {
            return CreateController(out logRepo, out enrollmentRepo, out enrollmentAssignmentRepo, out _);
        }

        private static LearningLogsController CreateController(
            out InMemoryGenericRepository<LearningLog> logRepo,
            out InMemoryGenericRepository<Enrollment> enrollmentRepo,
            out InMemoryGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepo,
            out FakeScormRuntimeStateService runtimeStateService)
        {
            enrollmentRepo = new InMemoryGenericRepository<Enrollment>(
            [
                new Enrollment
                {
                    Id = 10,
                    LearnerCode = "490222",
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
                    CourseContentItems =
                    [
                        CreateCourseContentItem(1, 100, 2),
                        CreateCourseContentItem(2, 101, 1)
                    ]
                }
            ]);

            logRepo = new InMemoryGenericRepository<LearningLog>([]);
            enrollmentAssignmentRepo = new InMemoryGenericRepository<EnrollmentAssignment>([]);
            runtimeStateService = new FakeScormRuntimeStateService();

            var controller = new LearningLogsController(
                logRepo,
                enrollmentRepo,
                versionRepo,
                enrollmentAssignmentRepo,
                new FakeCurrentUserService(),
                new MemoryCache(new MemoryCacheOptions()),
                new FakeLearnerProxyIdentityResolver(),
                runtimeStateService,
                new FakeDateTime(),
                NullLogger<LearningLogsController>.Instance);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        private static CourseContentItem CreateCourseContentItem(int id, int contentItemId, int typeId)
        {
            return new CourseContentItem
            {
                Id = id,
                ContentItemId = contentItemId,
                ContentItem = new ContentItem
                {
                    Id = contentItemId,
                    TypeId = typeId,
                    Name = typeId == ScormContentStatusPolicy.ExamTypeId ? $"Exam {contentItemId}" : $"Learn {contentItemId}"
                }
            };
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
            public bool TryResolveLearnerCode(HttpContext context, out string learnerCode, out int statusCode, out string errorMessage)
            {
                learnerCode = "490222";
                statusCode = StatusCodes.Status200OK;
                errorMessage = string.Empty;
                return true;
            }
        }

        private sealed class FakeScormRuntimeStateService : IScormRuntimeStateService
        {
            public int? ClearedEnrollmentId { get; private set; }

            public Task<IReadOnlyList<ScormRuntimeStateDto>> GetActiveStatesAsync(int enrollmentId, DateTime? resetAt = null)
            {
                return Task.FromResult<IReadOnlyList<ScormRuntimeStateDto>>([]);
            }

            public Task<int> ClearForEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken = default)
            {
                ClearedEnrollmentId = enrollmentId;
                return Task.FromResult(0);
            }

            public Task<int> ClearForEnrollmentsAsync(IReadOnlyCollection<int> enrollmentIds, bool saveChanges = true, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(0);
            }

            public Task<IReadOnlyList<ScormRuntimeStateDto>> UpsertAsync(int enrollmentId, IReadOnlyCollection<ScormRuntimeContentItemCommitDto> contentItems, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<ScormRuntimeStateDto>>(
                    contentItems.Select(contentItem => new ScormRuntimeStateDto
                    {
                        EnrollmentId = enrollmentId,
                        ContentItemId = contentItem.ContentItemId,
                        ScormVersion = contentItem.ScormVersion
                    }).ToList());
            }
        }

        private sealed class FakeDateTime : IDateTime
        {
            public DateTime Now => new(2026, 4, 28, 12, 0, 0, DateTimeKind.Local);
            public System.Globalization.CultureInfo CultureInfo => System.Globalization.CultureInfo.InvariantCulture;
            public DateTime UnixTime => new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
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