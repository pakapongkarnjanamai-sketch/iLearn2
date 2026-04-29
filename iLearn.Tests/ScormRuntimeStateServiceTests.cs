using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Services;
using System.Globalization;
using System.Linq.Expressions;

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
        public async Task UpsertAsync_AllowsMeaningfulIncompleteCommitToOverrideTerminalState()
        {
            var existingState = new ScormRuntimeState
            {
                Id = 1,
                EnrollmentId = 11192,
                ContentItemId = 1089,
                ScormVersion = ScormRuntimeFieldMap.Scorm12,
                LessonStatus = "completed",
                CompletionStatus = "completed",
                SuccessStatus = "unknown",
                RawScore = 100m,
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
                    CompletionStatus = "completed",
                    SuccessStatus = "unknown",
                    RawScore = 20m,
                    SessionTime = "00:00:00",
                    TotalTime = "00:00:00",
                    Exit = "suspend",
                    LastCommittedAtUtc = Now
                }
            ]);

            var updated = Assert.Single(result);

            Assert.Equal("incomplete", updated.LessonStatus);
            Assert.Equal("incomplete", updated.CompletionStatus);
            Assert.Equal("unknown", updated.SuccessStatus);
            Assert.Equal("N4IgDiBcCMA0IFsoCZ4DcoG0AMBdAvkA", updated.SuspendData);
            Assert.Equal(20m, updated.RawScore);
            Assert.Equal("suspend", updated.Exit);
            Assert.Equal(Now, updated.LastCommittedAtUtc);
            Assert.Equal(1, unitOfWork.SaveCallCount);
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
            public int SaveCallCount { get; private set; }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveCallCount++;
                return Task.FromResult(0);
            }

            public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
                => throw new NotSupportedException("Transactions are not exercised by these unit tests.");

            public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
                where T : BaseEntity
                => Task.CompletedTask;

            public void Dispose()
            {
            }
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