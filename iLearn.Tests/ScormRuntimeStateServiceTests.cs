using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace iLearn.Tests
{
    public sealed class ScormRuntimeStateServiceTests
    {
        private static readonly DateTime Now = new(2026, 4, 27, 10, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData("SCORM 1.2", ScormRuntimeFieldMap.Scorm12)]
        [InlineData("SCORM 2004 4th Edition", ScormRuntimeFieldMap.Scorm2004)]
        [InlineData("custom-runtime", "custom-runtime")]
        public void NormalizeVersion_ReturnsCanonicalRuntimeVersion(string value, string expected)
        {
            var normalized = ScormRuntimeFieldMap.NormalizeVersion(value);

            Assert.Equal(expected, normalized);
        }

        [Fact]
        public void NormalizeStatuses_BackfillsScorm12LessonStatusInto2004Fields()
        {
            Assert.Equal("completed", ScormRuntimeFieldMap.NormalizeCompletionStatus("passed", null));
            Assert.Equal("completed", ScormRuntimeFieldMap.NormalizeCompletionStatus("failed", null));
            Assert.Equal("incomplete", ScormRuntimeFieldMap.NormalizeCompletionStatus("incomplete", null));
            Assert.Equal("already-complete", ScormRuntimeFieldMap.NormalizeCompletionStatus("passed", "already-complete"));

            Assert.Equal("passed", ScormRuntimeFieldMap.NormalizeSuccessStatus("passed", null));
            Assert.Equal("failed", ScormRuntimeFieldMap.NormalizeSuccessStatus("failed", null));
            Assert.Null(ScormRuntimeFieldMap.NormalizeSuccessStatus("completed", null));
            Assert.Equal("unknown", ScormRuntimeFieldMap.NormalizeSuccessStatus("failed", "unknown"));
        }

        [Fact]
        public async Task UpsertAsync_NormalizesCommitAndPreservesExistingValuesWhenIncomingFieldsAreBlank()
        {
            var existingState = new ScormRuntimeState
            {
                Id = 1,
                EnrollmentId = 99,
                ContentItemId = 10,
                ScormVersion = ScormRuntimeFieldMap.Scorm12,
                LessonLocation = "page-1",
                SuspendData = "state-a",
                LessonStatus = "incomplete",
                CompletionStatus = "incomplete",
                SessionTime = "0000:00:30.00",
                TotalTime = "0000:05:00.00",
                Entry = "resume",
                Exit = "suspend",
                CmiSnapshotJson = "{\"cmi\":\"old\"}",
                CreatedAt = Now.AddMinutes(-30),
                UpdatedAt = Now.AddMinutes(-20),
                LastCommittedAtUtc = Now.AddMinutes(-20)
            };

            var repo = new InMemoryGenericRepository<ScormRuntimeState>([existingState], Now);
            var unitOfWork = new FakeUnitOfWork();
            var service = new ScormRuntimeStateService(repo, unitOfWork, new FakeDateTime(Now));

            var result = await service.UpsertAsync(99,
            [
                new ScormRuntimeContentItemCommitDto
                {
                    ContentItemId = 10,
                    ScormVersion = "SCORM 1.2",
                    LessonLocation = " ",
                    SuspendData = "state-b",
                    LessonStatus = "passed",
                    RawScore = 92.5m,
                    TotalTime = "0000:10:00.00",
                    Entry = " ",
                    Exit = "logout",
                    CmiSnapshotJson = " {\"cmi\":\"new\"} "
                }
            ]);

            var updated = Assert.Single(result);

            Assert.Equal(ScormRuntimeFieldMap.Scorm12, updated.ScormVersion);
            Assert.Equal("page-1", updated.LessonLocation);
            Assert.Equal("state-b", updated.SuspendData);
            Assert.Equal("passed", updated.LessonStatus);
            Assert.Equal("completed", updated.CompletionStatus);
            Assert.Equal("passed", updated.SuccessStatus);
            Assert.Equal(92.5m, updated.RawScore);
            Assert.Equal("0000:00:30.00", updated.SessionTime);
            Assert.Equal("0000:10:00.00", updated.TotalTime);
            Assert.Equal("resume", updated.Entry);
            Assert.Equal("logout", updated.Exit);
            Assert.Equal("{\"cmi\":\"new\"}", updated.CmiSnapshotJson);
            Assert.Equal(Now, updated.LastCommittedAtUtc);
            Assert.Equal(1, unitOfWork.SaveCallCount);
        }

        [Fact]
        public async Task UpsertAsync_PreservesResumeStateWhenIncomingCommitOnlyCarriesPlaceholderDefaults()
        {
            var existingState = new ScormRuntimeState
            {
                Id = 1,
                EnrollmentId = 11191,
                ContentItemId = 1088,
                ScormVersion = ScormRuntimeFieldMap.Scorm12,
                LessonLocation = "pg-4",
                SuspendData = "resume-token-123",
                LessonStatus = "incomplete",
                CompletionStatus = "incomplete",
                RawScore = 42m,
                SessionTime = "00:01:30",
                TotalTime = "00:01:30",
                Entry = "resume",
                Exit = "suspend",
                CmiSnapshotJson = "{\"cmi.core.lesson_location\":\"pg-4\",\"cmi.suspend_data\":\"resume-token-123\",\"cmi.core.score.raw\":\"42\",\"cmi.core.entry\":\"resume\"}",
                CreatedAt = Now.AddMinutes(-10),
                UpdatedAt = Now.AddMinutes(-5),
                LastCommittedAtUtc = Now.AddMinutes(-5)
            };

            var repo = new InMemoryGenericRepository<ScormRuntimeState>([existingState], Now);
            var unitOfWork = new FakeUnitOfWork();
            var service = new ScormRuntimeStateService(repo, unitOfWork, new FakeDateTime(Now));

            var result = await service.UpsertAsync(11191,
            [
                new ScormRuntimeContentItemCommitDto
                {
                    ContentItemId = 1088,
                    ScormVersion = "1.2",
                    LessonStatus = "incomplete",
                    CompletionStatus = "incomplete",
                    SuccessStatus = "unknown",
                    RawScore = 0m,
                    SessionTime = "00:00:00",
                    TotalTime = "00:00:00",
                    Entry = "ab-initio",
                    Exit = "suspend",
                    CmiSnapshotJson = "{\"cmi.core.lesson_location\":\"\",\"cmi.location\":\"\",\"cmi.suspend_data\":\"\",\"cmi.core.score.raw\":\"0\",\"cmi.core.entry\":\"ab-initio\"}",
                    LastCommittedAtUtc = Now
                }
            ]);

            var updated = Assert.Single(result);

            Assert.Equal("pg-4", updated.LessonLocation);
            Assert.Equal("resume-token-123", updated.SuspendData);
            Assert.Equal(42m, updated.RawScore);
            Assert.Equal("00:01:30", updated.SessionTime);
            Assert.Equal("00:01:30", updated.TotalTime);
            Assert.Equal("resume", updated.Entry);
            Assert.Equal("suspend", updated.Exit);
            Assert.Equal("unknown", updated.SuccessStatus);
            Assert.Equal(Now, updated.LastCommittedAtUtc);
            Assert.Equal(1, unitOfWork.SaveCallCount);
        }

        [Fact]
        public async Task UpsertAsync_PreservesTerminalStateWhenIncomingPlaceholderCarriesSessionTime()
        {
            var existingState = new ScormRuntimeState
            {
                Id = 1,
                EnrollmentId = 11192,
                ContentItemId = 1089,
                ScormVersion = ScormRuntimeFieldMap.Scorm12,
                LessonStatus = "completed",
                CompletionStatus = "completed",
            SuccessStatus = "passed",
                RawScore = 100m,
            SessionTime = "00:01:20",
                CreatedAt = Now.AddMinutes(-10),
                UpdatedAt = Now.AddMinutes(-5),
                LastCommittedAtUtc = Now.AddMinutes(-5)
            };

            var repo = new InMemoryGenericRepository<ScormRuntimeState>([existingState], Now);
            var unitOfWork = new FakeUnitOfWork();
            var service = new ScormRuntimeStateService(repo, unitOfWork, new FakeDateTime(Now));

            var result = await service.UpsertAsync(11192,
            [
                new ScormRuntimeContentItemCommitDto
                {
                    ContentItemId = 1089,
                    ScormVersion = "1.2",
                    SuspendData = "N4IgDiBcCMA0IFsoCZ4DcoG0AMBdAvkA",
                    LessonStatus = "incomplete",
                    CompletionStatus = "incomplete",
                    SuccessStatus = "unknown",
                    RawScore = 0m,
                    SessionTime = "02:33:55",
                    Exit = "suspend",
                    LastCommittedAtUtc = Now
                }
            ]);

            var updated = Assert.Single(result);

            Assert.Equal("completed", updated.LessonStatus);
            Assert.Equal("completed", updated.CompletionStatus);
            Assert.Equal("passed", updated.SuccessStatus);
            Assert.Equal("N4IgDiBcCMA0IFsoCZ4DcoG0AMBdAvkA", updated.SuspendData);
            Assert.Equal(100m, updated.RawScore);
            Assert.Equal("02:33:55", updated.SessionTime);
            Assert.Equal("suspend", updated.Exit);
            Assert.Equal(Now, updated.LastCommittedAtUtc);
            Assert.Equal(1, unitOfWork.SaveCallCount);
        }

        [Fact]
        public async Task UpsertAsync_AllowsTerminalStatusAndZeroScoreToOverridePreviousTerminalState()
        {
            var existingState = new ScormRuntimeState
            {
                Id = 1,
                EnrollmentId = 11193,
                ContentItemId = 1090,
                ScormVersion = ScormRuntimeFieldMap.Scorm12,
                LessonStatus = "passed",
                CompletionStatus = "completed",
                SuccessStatus = "passed",
                RawScore = 100m,
                CreatedAt = Now.AddMinutes(-10),
                UpdatedAt = Now.AddMinutes(-5),
                LastCommittedAtUtc = Now.AddMinutes(-5)
            };

            var repo = new InMemoryGenericRepository<ScormRuntimeState>([existingState], Now);
            var unitOfWork = new FakeUnitOfWork();
            var service = new ScormRuntimeStateService(repo, unitOfWork, new FakeDateTime(Now));

            var result = await service.UpsertAsync(11193,
            [
                new ScormRuntimeContentItemCommitDto
                {
                    ContentItemId = 1090,
                    ScormVersion = "1.2",
                    LessonStatus = "failed",
                    RawScore = 0m,
                    SessionTime = "00:02:00",
                    LastCommittedAtUtc = Now
                }
            ]);

            var updated = Assert.Single(result);

            Assert.Equal("failed", updated.LessonStatus);
            Assert.Equal("completed", updated.CompletionStatus);
            Assert.Equal("failed", updated.SuccessStatus);
            Assert.Equal(0m, updated.RawScore);
            Assert.Equal("00:02:00", updated.SessionTime);
            Assert.Equal(1, unitOfWork.SaveCallCount);
        }

        [Fact]
        public async Task UpsertAsync_WhenFirstInsertHitsUniqueViolation_ReloadsAndUpdatesWinningState()
        {
            var repo = new InMemoryGenericRepository<ScormRuntimeState>([], Now);
            ScormRuntimeState? winningState = null;
            var unitOfWork = new FakeUnitOfWork(
                saveFailures: [CreateSqlUpdateException(2601)],
                beforeSaveFailure: saveCallCount =>
                {
                    if (saveCallCount == 1)
                    {
                        winningState = new ScormRuntimeState
                        {
                            Id = 42,
                            EnrollmentId = 300,
                            ContentItemId = 30,
                            ScormVersion = ScormRuntimeFieldMap.Scorm12,
                            LessonLocation = "page-from-winning-request",
                            LessonStatus = "incomplete",
                            CompletionStatus = "incomplete",
                            SuccessStatus = "unknown",
                            RawScore = 15m,
                            CreatedAt = Now.AddMinutes(-1),
                            LastCommittedAtUtc = Now.AddMinutes(-1)
                        };
                        repo.Items.Add(winningState);
                    }
                },
                detachAction: entity => repo.Items.Remove((ScormRuntimeState)entity));
            var service = new ScormRuntimeStateService(repo, unitOfWork, new FakeDateTime(Now));

            var result = await service.UpsertAsync(300,
            [
                new ScormRuntimeContentItemCommitDto
                {
                    ContentItemId = 30,
                    ScormVersion = "1.2",
                    LessonLocation = " ",
                    LessonStatus = "passed",
                    RawScore = 88m,
                    SessionTime = "00:02:30",
                    LastCommittedAtUtc = Now
                }
            ]);

            var updated = Assert.Single(result);
            Assert.NotNull(winningState);
            Assert.Same(winningState, repo.Items.Single());
            Assert.Equal("page-from-winning-request", updated.LessonLocation);
            Assert.Equal("passed", updated.LessonStatus);
            Assert.Equal("completed", updated.CompletionStatus);
            Assert.Equal("passed", updated.SuccessStatus);
            Assert.Equal(88m, updated.RawScore);
            Assert.Equal("00:02:30", updated.SessionTime);
            Assert.Equal(Now, updated.LastCommittedAtUtc);
            Assert.Equal(2, unitOfWork.SaveCallCount);
            Assert.Equal(1, unitOfWork.DetachCallCount);
        }

        [Fact]
        public async Task UpsertAsync_WhenSaveFailsWithNonUniqueUpdateException_RethrowsWithoutRetry()
        {
            var repo = new InMemoryGenericRepository<ScormRuntimeState>([], Now);
            var failure = new DbUpdateException("Truncation or another non-unique database error.", new InvalidOperationException("not unique"));
            var unitOfWork = new FakeUnitOfWork(
                saveFailures: [failure],
                detachAction: entity => repo.Items.Remove((ScormRuntimeState)entity));
            var service = new ScormRuntimeStateService(repo, unitOfWork, new FakeDateTime(Now));

            var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => service.UpsertAsync(301,
            [
                new ScormRuntimeContentItemCommitDto
                {
                    ContentItemId = 31,
                    ScormVersion = "1.2",
                    LessonStatus = "incomplete"
                }
            ]));

            Assert.Same(failure, thrown);
            Assert.Equal(1, unitOfWork.SaveCallCount);
            Assert.Equal(0, unitOfWork.DetachCallCount);
        }

        [Fact]
        public async Task UpsertAsync_WhenRetryStillFails_RethrowsAfterSingleRetry()
        {
            var repo = new InMemoryGenericRepository<ScormRuntimeState>([], Now);
            ScormRuntimeState? winningState = null;
            var firstFailure = CreateSqlUpdateException(2601);
            var secondFailure = CreateSqlUpdateException(2627);
            var unitOfWork = new FakeUnitOfWork(
                saveFailures: [firstFailure, secondFailure],
                beforeSaveFailure: saveCallCount =>
                {
                    if (saveCallCount == 1)
                    {
                        winningState = new ScormRuntimeState
                        {
                            Id = 43,
                            EnrollmentId = 302,
                            ContentItemId = 32,
                            ScormVersion = ScormRuntimeFieldMap.Scorm12,
                            LessonStatus = "incomplete",
                            CompletionStatus = "incomplete",
                            CreatedAt = Now.AddMinutes(-1)
                        };
                        repo.Items.Add(winningState);
                    }
                },
                detachAction: entity => repo.Items.Remove((ScormRuntimeState)entity));
            var service = new ScormRuntimeStateService(repo, unitOfWork, new FakeDateTime(Now));

            var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => service.UpsertAsync(302,
            [
                new ScormRuntimeContentItemCommitDto
                {
                    ContentItemId = 32,
                    ScormVersion = "1.2",
                    LessonStatus = "passed",
                    RawScore = 90m
                }
            ]));

            Assert.Same(secondFailure, thrown);
            Assert.NotNull(winningState);
            Assert.Equal(2, unitOfWork.SaveCallCount);
            Assert.Equal(1, unitOfWork.DetachCallCount);
        }

        [Fact]
        public async Task GetActiveStatesAsync_ExcludesStatesCommittedBeforeEnrollmentReset()
        {
            var resetAt = Now.AddHours(-1);
            var repo = new InMemoryGenericRepository<ScormRuntimeState>(
            [
                new ScormRuntimeState
                {
                    Id = 1,
                    EnrollmentId = 77,
                    ContentItemId = 1,
                    ScormVersion = ScormRuntimeFieldMap.Scorm12,
                    LessonLocation = "before-reset",
                    CreatedAt = Now.AddHours(-3),
                    UpdatedAt = Now.AddHours(-2)
                },
                new ScormRuntimeState
                {
                    Id = 2,
                    EnrollmentId = 77,
                    ContentItemId = 2,
                    ScormVersion = ScormRuntimeFieldMap.Scorm2004,
                    LessonLocation = "after-reset-updated",
                    CreatedAt = Now.AddHours(-3),
                    UpdatedAt = Now.AddMinutes(-30)
                },
                new ScormRuntimeState
                {
                    Id = 3,
                    EnrollmentId = 77,
                    ContentItemId = 3,
                    ScormVersion = ScormRuntimeFieldMap.Scorm12,
                    LessonLocation = "after-reset-created",
                    CreatedAt = Now.AddMinutes(-10)
                },
                new ScormRuntimeState
                {
                    Id = 4,
                    EnrollmentId = 88,
                    ContentItemId = 4,
                    ScormVersion = ScormRuntimeFieldMap.Scorm12,
                    LessonLocation = "other-enrollment",
                    CreatedAt = Now.AddMinutes(-5),
                    UpdatedAt = Now.AddMinutes(-5)
                }
            ],
            Now);

            var service = new ScormRuntimeStateService(repo, new FakeUnitOfWork(), new FakeDateTime(Now));

            var activeStates = await service.GetActiveStatesAsync(77, resetAt);

            Assert.Collection(activeStates,
                state =>
                {
                    Assert.Equal(2, state.ContentItemId);
                    Assert.Equal("after-reset-updated", state.LessonLocation);
                },
                state =>
                {
                    Assert.Equal(3, state.ContentItemId);
                    Assert.Equal("after-reset-created", state.LessonLocation);
                });
        }

        [Fact]
        public async Task ClearForEnrollmentAsync_SoftDeletesStatesAndRemovesThemFromActiveResults()
        {
            var states = new[]
            {
                new ScormRuntimeState
                {
                    Id = 1,
                    EnrollmentId = 123,
                    ContentItemId = 10,
                    ScormVersion = ScormRuntimeFieldMap.Scorm12,
                    CreatedAt = Now
                },
                new ScormRuntimeState
                {
                    Id = 2,
                    EnrollmentId = 123,
                    ContentItemId = 11,
                    ScormVersion = ScormRuntimeFieldMap.Scorm2004,
                    CreatedAt = Now
                },
                new ScormRuntimeState
                {
                    Id = 3,
                    EnrollmentId = 124,
                    ContentItemId = 12,
                    ScormVersion = ScormRuntimeFieldMap.Scorm12,
                    CreatedAt = Now
                }
            };
            var repo = new InMemoryGenericRepository<ScormRuntimeState>(states, Now);
            var unitOfWork = new FakeUnitOfWork();
            var service = new ScormRuntimeStateService(repo, unitOfWork, new FakeDateTime(Now));

            var clearedCount = await service.ClearForEnrollmentAsync(123);

            Assert.Equal(2, clearedCount);
            Assert.All(repo.Items.Where(state => state.EnrollmentId == 123), state => Assert.True(state.IsDeleted));
            Assert.False(repo.Items.Single(state => state.EnrollmentId == 124).IsDeleted);
            Assert.Empty(await service.GetActiveStatesAsync(123));
            Assert.Equal(1, unitOfWork.SaveCallCount);
        }

        [Fact]
        public async Task ClearForEnrollmentsAsync_SoftDeletesRequestedStatesWithoutSavingWhenCallerOwnsCommit()
        {
            var states = new[]
            {
                new ScormRuntimeState { Id = 1, EnrollmentId = 123, ContentItemId = 10, CreatedAt = Now },
                new ScormRuntimeState { Id = 2, EnrollmentId = 124, ContentItemId = 11, CreatedAt = Now },
                new ScormRuntimeState { Id = 3, EnrollmentId = 125, ContentItemId = 12, CreatedAt = Now }
            };
            var repo = new InMemoryGenericRepository<ScormRuntimeState>(states, Now);
            var unitOfWork = new FakeUnitOfWork();
            var service = new ScormRuntimeStateService(repo, unitOfWork, new FakeDateTime(Now));

            var clearedCount = await service.ClearForEnrollmentsAsync([123, 124], saveChanges: false);

            Assert.Equal(2, clearedCount);
            Assert.All(repo.Items.Where(state => state.EnrollmentId is 123 or 124), state => Assert.True(state.IsDeleted));
            Assert.False(repo.Items.Single(state => state.EnrollmentId == 125).IsDeleted);
            Assert.Equal(0, unitOfWork.SaveCallCount);
        }

        [Fact]
        public async Task ClearForEnrollmentsAsync_SavesOnceWhenRequested()
        {
            var repo = new InMemoryGenericRepository<ScormRuntimeState>(
            [
                new ScormRuntimeState { Id = 1, EnrollmentId = 123, ContentItemId = 10, CreatedAt = Now }
            ], Now);
            var unitOfWork = new FakeUnitOfWork();
            var service = new ScormRuntimeStateService(repo, unitOfWork, new FakeDateTime(Now));

            var clearedCount = await service.ClearForEnrollmentsAsync([123]);

            Assert.Equal(1, clearedCount);
            Assert.Equal(1, unitOfWork.SaveCallCount);
        }

        [Fact]
        public async Task ResetStatusAsync_ClearsRuntimeStateAndAssignmentSnapshots()
        {
            var enrollment = new Enrollment
            {
                Id = 123,
                IsCompleted = true,
                CompletedDate = Now.AddDays(-1),
                Progress = 100,
                ResetAt = Now.AddDays(-2)
            };
            var enrollmentRepo = new InMemoryGenericRepository<Enrollment>([enrollment], Now);
            var assignmentLink = new EnrollmentAssignment
            {
                Id = 1,
                EnrollmentId = enrollment.Id,
                SnapshotCompleted = true,
                SnapshotCompletedDate = Now.AddDays(-1),
                SnapshotProgress = 100
            };
            var assignmentRepo = new InMemoryGenericRepository<EnrollmentAssignment>([assignmentLink], Now);
            var runtimeStateService = new RecordingScormRuntimeStateService();
            var service = new EnrollmentService(
                enrollmentRepo,
                assignmentRepo,
                runtimeStateService,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                new FakeDateTime(Now),
                new FakeUnitOfWork());

            var result = await service.ResetStatusAsync(enrollment.Id);

            Assert.NotNull(result);
            Assert.False(enrollment.IsCompleted);
            Assert.Null(enrollment.CompletedDate);
            Assert.Equal(0, enrollment.Progress);
            Assert.Equal(Now, enrollment.ResetAt);
            Assert.False(assignmentLink.SnapshotCompleted);
            Assert.Null(assignmentLink.SnapshotCompletedDate);
            Assert.Equal(0, assignmentLink.SnapshotProgress);
            Assert.Equal(enrollment.Id, runtimeStateService.ClearedEnrollmentId);
        }

        [Fact]
        public async Task ClearForEnrollmentAsync_AllowsNextCommitToCreateCleanRuntimeState()
        {
            var previousState = new ScormRuntimeState
            {
                Id = 1,
                EnrollmentId = 125,
                ContentItemId = 10,
                ScormVersion = ScormRuntimeFieldMap.Scorm2004,
                LessonStatus = "completed",
                CompletionStatus = "completed",
                SuccessStatus = "passed",
                RawScore = 100m,
                CreatedAt = Now.AddHours(-1),
                UpdatedAt = Now.AddMinutes(-30)
            };
            var repo = new InMemoryGenericRepository<ScormRuntimeState>([previousState], Now);
            var service = new ScormRuntimeStateService(repo, new FakeUnitOfWork(), new FakeDateTime(Now));

            await service.ClearForEnrollmentAsync(125);
            var result = await service.UpsertAsync(125,
            [
                new ScormRuntimeContentItemCommitDto
                {
                    ContentItemId = 10,
                    ScormVersion = ScormRuntimeFieldMap.Scorm2004,
                    CompletionStatus = "incomplete",
                    SuccessStatus = "unknown",
                    RawScore = 0m,
                    SessionTime = "PT0S"
                }
            ]);

            var newState = Assert.Single(result);
            Assert.True(previousState.IsDeleted);
            Assert.Equal(2, repo.Items.Count);
            Assert.Equal("incomplete", newState.CompletionStatus);
            Assert.Equal("unknown", newState.SuccessStatus);
            Assert.Equal(0m, newState.RawScore);
        }

        private sealed class FakeDateTime : IDateTime
        {
            public FakeDateTime(DateTime now)
            {
                Now = now;
            }

            public DateTime Now { get; }
            public CultureInfo CultureInfo => CultureInfo.InvariantCulture;
            public DateTime UnixTime => Now;
        }

        private sealed class FakeUnitOfWork : IUnitOfWork
        {
            private readonly Queue<Exception> _saveFailures;
            private readonly Action<int>? _beforeSaveFailure;
            private readonly Action<BaseEntity>? _detachAction;

            public FakeUnitOfWork(
                IEnumerable<Exception>? saveFailures = null,
                Action<int>? beforeSaveFailure = null,
                Action<BaseEntity>? detachAction = null)
            {
                _saveFailures = new Queue<Exception>(saveFailures ?? []);
                _beforeSaveFailure = beforeSaveFailure;
                _detachAction = detachAction;
            }

            public int SaveCallCount { get; private set; }
            public int DetachCallCount { get; private set; }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveCallCount++;

                if (_saveFailures.Count > 0)
                {
                    var failure = _saveFailures.Dequeue();
                    _beforeSaveFailure?.Invoke(SaveCallCount);
                    throw failure;
                }

                return Task.FromResult(0);
            }

            public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
                => throw new NotSupportedException("Transactions are not exercised by these unit tests.");

            public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
                where T : BaseEntity
                => Task.CompletedTask;

            public void Detach<T>(T entity) where T : BaseEntity
            {
                DetachCallCount++;
                _detachAction?.Invoke(entity);
            }

            public void Dispose()
            {
            }
        }

        private static DbUpdateException CreateSqlUpdateException(int sqlErrorNumber)
        {
            return new DbUpdateException("Simulated SQL Server update failure.", CreateSqlException(sqlErrorNumber));
        }

        private static SqlException CreateSqlException(int sqlErrorNumber)
        {
            var errorCollection = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
            var error = CreateSqlError(sqlErrorNumber);
            typeof(SqlErrorCollection)
                .GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(errorCollection, [error]);

            var constructor = typeof(SqlException)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .First(ctor => ctor.GetParameters().Any(parameter => parameter.ParameterType == typeof(SqlErrorCollection)));
            var arguments = constructor
                .GetParameters()
                .Select(parameter => CreateSqlExceptionArgument(parameter, errorCollection))
                .ToArray();

            return (SqlException)constructor.Invoke(arguments);
        }

        private static SqlError CreateSqlError(int sqlErrorNumber)
        {
            foreach (var constructor in typeof(SqlError)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .OrderByDescending(ctor => ctor.GetParameters().Length))
            {
                var parameters = constructor.GetParameters();
                var arguments = new object?[parameters.Length];
                var assignedSqlErrorNumber = false;

                for (var index = 0; index < parameters.Length; index++)
                {
                    arguments[index] = CreateSqlErrorArgument(parameters[index], sqlErrorNumber, ref assignedSqlErrorNumber);
                }

                try
                {
                    return (SqlError)constructor.Invoke(arguments);
                }
                catch (TargetInvocationException)
                {
                }
            }

            throw new InvalidOperationException("Unable to create SqlError for test.");
        }

        private static object? CreateSqlErrorArgument(
            ParameterInfo parameter,
            int sqlErrorNumber,
            ref bool assignedSqlErrorNumber)
        {
            if (parameter.ParameterType == typeof(int))
            {
                if (!assignedSqlErrorNumber)
                {
                    assignedSqlErrorNumber = true;
                    return sqlErrorNumber;
                }

                return 0;
            }

            return CreateDefaultSqlReflectionArgument(parameter, null);
        }

        private static object? CreateSqlExceptionArgument(ParameterInfo parameter, SqlErrorCollection errorCollection)
        {
            return CreateDefaultSqlReflectionArgument(parameter, errorCollection);
        }

        private static object? CreateDefaultSqlReflectionArgument(ParameterInfo parameter, SqlErrorCollection? errorCollection)
        {
            var parameterType = parameter.ParameterType;
            if (parameterType == typeof(SqlErrorCollection))
            {
                return errorCollection;
            }

            if (parameterType == typeof(string))
            {
                return parameter.Name switch
                {
                    "server" => "test-sql-server",
                    "procedure" => "test-procedure",
                    _ => "Simulated SQL exception"
                };
            }

            if (parameterType == typeof(byte))
            {
                return (byte)0;
            }

            if (parameterType == typeof(uint))
            {
                return 0u;
            }

            if (parameterType == typeof(long))
            {
                return 0L;
            }

            if (parameterType == typeof(bool))
            {
                return false;
            }

            if (parameterType == typeof(Guid))
            {
                return Guid.NewGuid();
            }

            if (parameterType == typeof(Exception))
            {
                return null;
            }

            return parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
        }

        private sealed class RecordingScormRuntimeStateService : IScormRuntimeStateService
        {
            public int? ClearedEnrollmentId { get; private set; }

            public Task<IReadOnlyList<ScormRuntimeStateDto>> GetActiveStatesAsync(int enrollmentId, DateTime? resetAt = null) =>
                Task.FromResult<IReadOnlyList<ScormRuntimeStateDto>>([]);

            public Task<int> ClearForEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken = default)
            {
                ClearedEnrollmentId = enrollmentId;
                return Task.FromResult(0);
            }

            public Task<int> ClearForEnrollmentsAsync(IReadOnlyCollection<int> enrollmentIds, bool saveChanges = true, CancellationToken cancellationToken = default) =>
                Task.FromResult(0);

            public Task<IReadOnlyList<ScormRuntimeStateDto>> UpsertAsync(int enrollmentId, IReadOnlyCollection<ScormRuntimeContentItemCommitDto> contentItems, CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyList<ScormRuntimeStateDto>>([]);
        }

        private sealed class InMemoryGenericRepository<T> : IGenericRepository<T> where T : BaseEntity
        {
            private readonly DateTime _now;
            private int _nextId;

            public InMemoryGenericRepository(IEnumerable<T> items, DateTime now)
            {
                Items = items.ToList();
                _now = now;
                _nextId = Items.Count == 0 ? 1 : Items.Max(entity => entity.Id) + 1;
            }

            public List<T> Items { get; }

            public Task<IReadOnlyList<T>> GetAllAsync()
            {
                return Task.FromResult<IReadOnlyList<T>>(Items.Where(entity => !entity.IsDeleted).ToList());
            }

            public Task<T?> GetByIdAsync(int id)
            {
                return Task.FromResult(Items.FirstOrDefault(entity => entity.Id == id && !entity.IsDeleted));
            }

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

            public Task HardDeleteAsync(T entity)
            {
                Items.Remove(entity);
                return Task.CompletedTask;
            }

            public IQueryable<T> GetQuery()
            {
                return Items.Where(entity => !entity.IsDeleted).AsQueryable();
            }

            public Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, bool ignoreQueryFilters = false)
            {
                var result = ApplyFilter(filter, ignoreQueryFilters).ToList();
                return Task.FromResult<IReadOnlyList<T>>(result);
            }

            public Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
            {
                return Task.FromResult(ApplyFilter(filter).Count());
            }

            public Task<IEnumerable<TResult>> GetAsync<TResult>(Expression<Func<T, bool>>? filter = null, Expression<Func<T, TResult>>? selector = null)
            {
                if (selector == null)
                {
                    throw new ArgumentException("Selector is required", nameof(selector));
                }

                var result = ApplyFilter(filter).Select(selector.Compile()).ToList();
                return Task.FromResult<IEnumerable<TResult>>(result);
            }

            private IEnumerable<T> ApplyFilter(Expression<Func<T, bool>>? filter, bool ignoreQueryFilters = false)
            {
                var query = ignoreQueryFilters ? Items.AsEnumerable() : Items.Where(entity => !entity.IsDeleted);
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