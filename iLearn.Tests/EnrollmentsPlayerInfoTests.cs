using iLearn.API.Controllers;
using iLearn.API.Services;
using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;

namespace iLearn.Tests
{
    public sealed class EnrollmentsPlayerInfoTests
    {
        private static readonly DateTime Now = new(2026, 4, 27, 14, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task GetPlayerInfoByCourse_ClosedAssignedCourse_ReturnsPersistedRuntimeStateForResume()
        {
            var runtimeStateService = new FakeScormRuntimeStateService(
            [
                new ScormRuntimeStateDto
                {
                    EnrollmentId = 10,
                    ContentItemId = 100,
                    ScormVersion = ScormRuntimeFieldMap.Scorm2004,
                    LessonLocation = "page-7",
                    SuspendData = "bookmark-state",
                    LessonStatus = "incomplete",
                    CompletionStatus = "incomplete",
                    RawScore = 60,
                    CmiSnapshotJson = "{\"debug\":true}"
                }
            ]);

            var controller = CreateController(runtimeStateService,
                enrollments:
                [
                    new Enrollment
                    {
                        Id = 10,
                        CourseId = 5,
                        LearnerCode = "490222",
                        EnrolledCourseVersion = 20,
                        ResetAt = Now.AddMinutes(-30),
                        Progress = 0,
                        IsCompleted = false,
                        Course = new Course
                        {
                            Id = 5,
                            Code = "C-05",
                            Title = "Safety Course",
                            Status = CourseStatus.Closed,
                            Category = new Category { Id = 3, Name = "Safety" },
                            CourseType = new CourseType { Id = 4, Name = "Common" }
                        }
                    }
                ],
                versions:
                [
                    new CourseVersion
                    {
                        Id = 20,
                        CourseId = 5,
                        VersionNumber = 2,
                        Course = new Course
                        {
                            Id = 5,
                            Code = "C-05",
                            Title = "Safety Course",
                            Status = CourseStatus.Closed,
                            Category = new Category { Id = 3, Name = "Safety" },
                            CourseType = new CourseType { Id = 4, Name = "Common" }
                        },
                        CourseContentItems =
                        [
                            new CourseContentItem
                            {
                                Id = 1,
                                ContentItemId = 100,
                                ContentItem = new ContentItem
                                {
                                    Id = 100,
                                    Name = "SCORM Learn",
                                    TypeId = 1,
                                    URL = "pkg-1",
                                    LaunchHref = "launch/index.html",
                                    SchemaVersion = "SCORM 1.2"
                                }
                            }
                        ]
                    }
                ],
                logs:
                [
                    new LearningLog
                    {
                        Id = 1,
                        EnrollmentId = 10,
                        LearnerCode = "490222",
                        CourseVersionId = 20,
                        ContentItemId = 100,
                        Status = "incomplete",
                        SessionTime = "00:05:00",
                        Score = 35,
                        CreatedAt = Now
                    }
                ]);

            var result = await controller.GetPlayerInfoByCourse(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PlayerInfoDto>>(ok.Value);
            var dto = Assert.IsType<PlayerInfoDto>(response.Data);
            var contentItem = Assert.Single(dto.ContentItems);

            Assert.True(response.Success);
            Assert.Equal(10, dto.EnrollmentId);
            Assert.False(dto.IsReadOnly);
            Assert.Equal(20, dto.CourseVersionId);
            Assert.Equal("Safety Course", dto.CourseTitle);
            Assert.Equal("Safety", dto.CategoryName);
            Assert.Equal("Common", dto.CourseTypeName);
            Assert.Equal(0, dto.Progress);
            Assert.Equal("https://files.example.local/course/pkg-1/launch/index.html", contentItem.LaunchUrl);
            Assert.Equal(ScormRuntimeFieldMap.Scorm2004, contentItem.ScormVersion);
            Assert.Equal("incomplete", contentItem.Status);
            Assert.Equal(0, contentItem.Progress);
            Assert.Equal(60, contentItem.ActivityProgress);
            Assert.Equal(60m, contentItem.Score);
            Assert.Equal("00:05:00", contentItem.Time);
            Assert.NotNull(contentItem.RuntimeState);
            Assert.Equal("page-7", contentItem.RuntimeState!.LessonLocation);
            Assert.Equal("bookmark-state", contentItem.RuntimeState.SuspendData);
            Assert.Equal(10, runtimeStateService.LastEnrollmentId);
            Assert.Equal(Now.AddMinutes(-30), runtimeStateService.LastResetAt);

            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.DoesNotContain("cmiSnapshotJson", json);
        }

        [Fact]
        public async Task GetPlayerInfoByCourse_WithoutEnrollment_FallsBackToActiveVersionInReadOnlyMode()
        {
            var controller = CreateController(new FakeScormRuntimeStateService([]),
                enrollments: [],
                versions:
                [
                    new CourseVersion
                    {
                        Id = 30,
                        CourseId = 9,
                        VersionNumber = 3,
                        IsActive = true,
                        Course = new Course { Id = 9, Code = "C-09", Title = "Preview Course", Status = CourseStatus.Open },
                        CourseContentItems =
                        [
                            new CourseContentItem
                            {
                                Id = 2,
                                ContentItemId = 200,
                                ContentItem = new ContentItem
                                {
                                    Id = 200,
                                    Name = "Preview ContentItem",
                                    TypeId = 1,
                                    URL = "https://cdn.example.local/preview/index.html",
                                    LaunchHref = null,
                                    SchemaVersion = "SCORM 1.2"
                                }
                            }
                        ]
                    }
                ],
                logs: []);

            var result = await controller.GetPlayerInfoByCourse(9);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PlayerInfoDto>>(ok.Value);
            var dto = Assert.IsType<PlayerInfoDto>(response.Data);
            var contentItem = Assert.Single(dto.ContentItems);

            Assert.True(dto.IsReadOnly);
            Assert.Null(dto.EnrollmentId);
            Assert.Equal(30, dto.CourseVersionId);
            Assert.Equal("Preview Course", dto.CourseTitle);
            Assert.Equal("https://cdn.example.local/preview/index.html", contentItem.LaunchUrl);
            Assert.Equal(ScormRuntimeFieldMap.Scorm12, contentItem.ScormVersion);
            Assert.Equal("incomplete", contentItem.Status);
            Assert.Equal(0, contentItem.Progress);
            Assert.Equal(0, contentItem.ActivityProgress);
            Assert.Equal("00:00:00", contentItem.Time);
            Assert.Null(contentItem.RuntimeState);
        }

        [Fact]
        public async Task GetPlayerInfoByCourse_UnreadyContentItem_ReturnsNotFound()
        {
            var controller = CreateController(new FakeScormRuntimeStateService([]),
                enrollments:
                [
                    new Enrollment
                    {
                        Id = 10,
                        CourseId = 5,
                        LearnerCode = "490222",
                        EnrolledCourseVersion = 20,
                        Course = new Course { Id = 5, Code = "C-05", Title = "Safety Course", Status = CourseStatus.Open }
                    }
                ],
                versions:
                [
                    new CourseVersion
                    {
                        Id = 20,
                        CourseId = 5,
                        VersionNumber = 2,
                        Course = new Course { Id = 5, Code = "C-05", Title = "Safety Course", Status = CourseStatus.Open },
                        CourseContentItems =
                        [
                            new CourseContentItem
                            {
                                Id = 1,
                                ContentItemId = 100,
                                ContentItem = new ContentItem
                                {
                                    Id = 100,
                                    Name = "Draft SCORM",
                                    TypeId = 1,
                                    IsActive = false,
                                    URL = null,
                                    LaunchHref = null
                                }
                            }
                        ]
                    }
                ],
                logs: []);

            var result = await controller.GetPlayerInfoByCourse(5);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(notFound.Value);
            Assert.False(response.Success);
            Assert.Equal("Content is not ready for learning.", response.Message);
        }

        [Fact]
        public async Task GetPlayerInfoByCourse_SeparatesCourseCompletionProgressFromContentItemActivityProgress()
        {
            var runtimeStateService = new FakeScormRuntimeStateService(
            [
                new ScormRuntimeStateDto
                {
                    EnrollmentId = 10,
                    ContentItemId = 100,
                    ScormVersion = ScormRuntimeFieldMap.Scorm12,
                    LessonStatus = "completed",
                    CompletionStatus = "completed",
                    RawScore = 0
                },
                new ScormRuntimeStateDto
                {
                    EnrollmentId = 10,
                    ContentItemId = 101,
                    ScormVersion = ScormRuntimeFieldMap.Scorm2004,
                    LessonStatus = "incomplete",
                    CompletionStatus = "incomplete",
                    RawScore = 60
                }
            ]);

            var controller = CreateController(runtimeStateService,
                enrollments:
                [
                    new Enrollment
                    {
                        Id = 10,
                        CourseId = 5,
                        LearnerCode = "490222",
                        EnrolledCourseVersion = 20,
                        Progress = 25,
                        IsCompleted = false,
                        Course = new Course { Id = 5, Code = "C-05", Title = "Safety Course", Status = CourseStatus.Open }
                    }
                ],
                versions:
                [
                    new CourseVersion
                    {
                        Id = 20,
                        CourseId = 5,
                        VersionNumber = 2,
                        Course = new Course { Id = 5, Code = "C-05", Title = "Safety Course", Status = CourseStatus.Open },
                        CourseContentItems =
                        [
                            CreateCourseContentItem(1, 100, "Learn 1.2", 1, "SCORM 1.2"),
                            CreateCourseContentItem(2, 101, "Learn 2004", 1, "SCORM 2004"),
                            CreateCourseContentItem(3, 102, "Exam 1.2", 2, "SCORM 1.2"),
                            CreateCourseContentItem(4, 103, "Exam 2004", 2, "SCORM 2004")
                        ]
                    }
                ],
                logs:
                [
                    new LearningLog
                    {
                        Id = 1,
                        EnrollmentId = 10,
                        LearnerCode = "490222",
                        CourseVersionId = 20,
                        ContentItemId = 100,
                        Status = "completed",
                        Progress = 100,
                        Score = 0,
                        CreatedAt = Now
                    },
                    new LearningLog
                    {
                        Id = 2,
                        EnrollmentId = 10,
                        LearnerCode = "490222",
                        CourseVersionId = 20,
                        ContentItemId = 101,
                        Status = "incomplete",
                        Progress = 0,
                        Score = 60,
                        CreatedAt = Now
                    }
                ]);

            var result = await controller.GetPlayerInfoByCourse(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PlayerInfoDto>>(ok.Value);
            var dto = Assert.IsType<PlayerInfoDto>(response.Data);

            Assert.Equal(25, dto.Progress);
            Assert.Collection(dto.ContentItems,
                first =>
                {
                    Assert.Equal("completed", first.Status);
                    Assert.Equal(100, first.Progress);
                    Assert.Equal(100, first.ActivityProgress);
                },
                second =>
                {
                    Assert.Equal("incomplete", second.Status);
                    Assert.Equal(0, second.Progress);
                    Assert.Equal(60, second.ActivityProgress);
                },
                third =>
                {
                    Assert.Equal("incomplete", third.Status);
                    Assert.Equal(0, third.Progress);
                    Assert.Equal(0, third.ActivityProgress);
                },
                fourth =>
                {
                    Assert.Equal("incomplete", fourth.Status);
                    Assert.Equal(0, fourth.Progress);
                    Assert.Equal(0, fourth.ActivityProgress);
                });
        }

        [Fact]
        public async Task GetPlayerInfoByCourse_ExamCompletedWithoutPass_RemainsIncomplete()
        {
            var runtimeStateService = new FakeScormRuntimeStateService(
            [
                new ScormRuntimeStateDto
                {
                    EnrollmentId = 10,
                    ContentItemId = 103,
                    ScormVersion = ScormRuntimeFieldMap.Scorm2004,
                    CompletionStatus = "completed",
                    SuccessStatus = "unknown",
                    RawScore = 75
                }
            ]);

            var controller = CreateController(runtimeStateService,
                enrollments:
                [
                    new Enrollment
                    {
                        Id = 10,
                        CourseId = 5,
                        LearnerCode = "490222",
                        EnrolledCourseVersion = 20,
                        Progress = 0,
                        IsCompleted = false,
                        Course = new Course { Id = 5, Code = "C-05", Title = "Safety Course", Status = CourseStatus.Open }
                    }
                ],
                versions:
                [
                    new CourseVersion
                    {
                        Id = 20,
                        CourseId = 5,
                        VersionNumber = 2,
                        Course = new Course { Id = 5, Code = "C-05", Title = "Safety Course", Status = CourseStatus.Open },
                        CourseContentItems =
                        [
                            CreateCourseContentItem(4, 103, "Exam 2004", ScormContentStatusPolicy.ExamTypeId, "SCORM 2004")
                        ]
                    }
                ],
                logs: []);

            var result = await controller.GetPlayerInfoByCourse(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<PlayerInfoDto>>(ok.Value);
            var dto = Assert.IsType<PlayerInfoDto>(response.Data);
            var contentItem = Assert.Single(dto.ContentItems);

            Assert.Equal("Exam", contentItem.Type);
            Assert.Equal("incomplete", contentItem.Status);
            Assert.Equal(0, contentItem.Progress);
            Assert.Equal(0, contentItem.ActivityProgress);
            Assert.Equal(75m, contentItem.Score);
        }

        private static EnrollmentsController CreateController(
            FakeScormRuntimeStateService runtimeStateService,
            IEnumerable<Enrollment> enrollments,
            IEnumerable<CourseVersion> versions,
            IEnumerable<LearningLog> logs)
        {
            var controller = new EnrollmentsController(
                new InMemoryGenericRepository<Enrollment>(enrollments),
                new InMemoryGenericRepository<Course>([]),
                new FakeEnrollmentService(),
                new InMemoryGenericRepository<LearningLog>(logs),
                new InMemoryGenericRepository<CourseVersion>(versions),
                new FakeScormService(),
                new FakeDateTime(Now),
                new MemoryCache(new MemoryCacheOptions()),
                new FakeLearnerProxyIdentityResolver(),
                runtimeStateService,
                new NullNotificationService(),
                new FakeCurrentUserService());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        private static CourseContentItem CreateCourseContentItem(int id, int contentItemId, string name, int typeId, string schemaVersion)
        {
            return new CourseContentItem
            {
                Id = id,
                ContentItemId = contentItemId,
                ContentItem = new ContentItem
                {
                    Id = contentItemId,
                    Name = name,
                    TypeId = typeId,
                    URL = $"pkg-{contentItemId}",
                    LaunchHref = "launch/index.html",
                    SchemaVersion = schemaVersion
                }
            };
        }

        private sealed class FakeScormRuntimeStateService : IScormRuntimeStateService
        {
            private readonly IReadOnlyList<ScormRuntimeStateDto> _states;

            public FakeScormRuntimeStateService(IReadOnlyList<ScormRuntimeStateDto> states)
            {
                _states = states;
            }

            public int? LastEnrollmentId { get; private set; }
            public DateTime? LastResetAt { get; private set; }

            public Task<IReadOnlyList<ScormRuntimeStateDto>> GetActiveStatesAsync(int enrollmentId, DateTime? resetAt = null)
            {
                LastEnrollmentId = enrollmentId;
                LastResetAt = resetAt;
                return Task.FromResult(_states);
            }

            public Task<int> ClearForEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(0);
            }

            public Task<IReadOnlyList<ScormRuntimeStateDto>> UpsertAsync(int enrollmentId, IReadOnlyCollection<ScormRuntimeContentItemCommitDto> contentItems, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<ScormRuntimeStateDto>>([]);
            }
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
            public CultureInfo CultureInfo => CultureInfo.InvariantCulture;
            public DateTime UnixTime => Now;
        }

        private sealed class FakeUnitOfWork : IUnitOfWork
        {
            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
            public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : BaseEntity => Task.CompletedTask;
            public void Dispose() { }
        }

        private sealed class FakeScormService : IScormService
        {
            public Task<ScormManifestDto> ExtractAndParseScormAsync(byte[] fileContent, string folderName) => throw new NotSupportedException();
            public Task<ScormManifestDto> ExtractAndParseScormFromFileAsync(string zipFilePath, string folderName) => throw new NotSupportedException();
            public Task<string> SavePackageToArchiveAsync(Stream stream, string archiveFileName) => throw new NotSupportedException();
            public void DeleteScormFolder(string folderName) => throw new NotSupportedException();
            public void DeleteArchiveFile(string storagePath) => throw new NotSupportedException();
            public string GetArchiveFullPath(string relativePath) => throw new NotSupportedException();
            public string GetScormUrl(string folderName, string launchHref) => $"https://files.example.local/course/{folderName}/{launchHref}";
            public (int FileCount, long TotalSize) GetFolderInfo(string folderName) => (0, 0);
        }

        private sealed class FakeAssignmentNoGenerator : IAssignmentNoGenerator
        {
            public Task<string> NextAsync() => Task.FromResult("AS-20260427-001");
        }

        private sealed class FakeCourseAssignmentService : ICourseAssignmentService
        {
            public Task AssignGeneralCoursesToNewUserAsync(string employeeId) => Task.CompletedTask;
            public Task AssignCourseToEmployees(int courseId, List<string> employeeCodes, DateTime? startDate, DateTime? dueDate, int? assignmentRuleId = null, bool forceReset = false) => Task.CompletedTask;
            public Task AssignCoursesToEmployees(IReadOnlyDictionary<int, int> assignmentRuleIdsByCourseId, List<string> employeeCodes, DateTime? startDate, DateTime? dueDate, bool forceReset = false) => Task.CompletedTask;
            public Task<List<AssignmentHistoryDto>> GetAssignmentHistoryAsync() => Task.FromResult(new List<AssignmentHistoryDto>());
            public Task<AssignmentConflictDto> CheckAssignmentConflictsAsync(int courseId, List<string> employeeCodes, DateTime startDate, DateTime dueDate) => throw new NotSupportedException();
        }

        private sealed class FakeAssignmentDashboardService : IAssignmentDashboardService
        {
            public Task<AssignmentDashboardDto?> GetDashboardAsync(int assignmentId) => Task.FromResult<AssignmentDashboardDto?>(null);
            public Task<ValidateBeforeAssignResult> ValidateBeforeAssignAsync(BulkAssignDto dto) => Task.FromResult(new ValidateBeforeAssignResult { Success = true });
            public Task<PagedResult<AssignmentHistoryDto>> GetAssignmentHistoryPagedAsync(PaginationParams p) => Task.FromResult(new PagedResult<AssignmentHistoryDto>());
            public Task<List<AssignmentGroupHistoryDto>> GetGroupHistoryAsync(int groupId) => Task.FromResult(new List<AssignmentGroupHistoryDto>());
            public Task ExtendDueDateAsync(int assignmentId, DateTime newDueDate) => Task.CompletedTask;
            public Task<List<LookupCourseDto>> GetLookupCoursesAsync() => Task.FromResult(new List<LookupCourseDto>());
        }

        private sealed class FakeLearnerGroupService : ILearnerGroupService
        {
            public Task<List<LearnerGroupDto>> GetAllAsync() => Task.FromResult(new List<LearnerGroupDto>());
            public Task<PagedResult<LearnerGroupDto>> GetPagedAsync(PaginationParams p) => Task.FromResult(new PagedResult<LearnerGroupDto>());
            public Task<LearnerGroupDetailDto?> GetByIdAsync(int id) => Task.FromResult<LearnerGroupDetailDto?>(null);
            public Task<LearnerGroupDto> CreateAsync(CreateLearnerGroupDto dto) => throw new NotSupportedException();
            public Task UpdateAsync(int id, UpdateLearnerGroupDto dto) => Task.CompletedTask;
            public Task DeleteAsync(int id) => Task.CompletedTask;
            public Task AddMembersAsync(int groupId, AddGroupMembersDto dto) => Task.CompletedTask;
            public Task<LearnerGroupAddMembersPreviewDto> PreviewAddMembersAsync(int groupId, LearnerGroupAddMembersOptionsDto dto) => throw new NotSupportedException();
            public Task<LearnerGroupAddMembersResultDto> AddMembersWithAssignmentsAsync(int groupId, LearnerGroupAddMembersOptionsDto dto) => throw new NotSupportedException();
            public Task<LearnerGroupRemoveMembersPreviewDto> PreviewRemoveMembersAsync(int groupId, LearnerGroupRemoveMembersOptionsDto dto) => throw new NotSupportedException();
            public Task<LearnerGroupRemoveMembersResultDto> RemoveMembersWithAssignmentsAsync(int groupId, LearnerGroupRemoveMembersOptionsDto dto) => throw new NotSupportedException();
            public Task RemoveMemberAsync(int groupId, int memberId) => Task.CompletedTask;
            public Task<List<string>> GetLearnerCodesAsync(int groupId) => Task.FromResult(new List<string>());
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
            public Task<T> AddAsync(T entity) { Items.Add(entity); return Task.FromResult(entity); }
            public Task<T> AddWithoutSaveAsync(T entity) => AddAsync(entity);
            public Task UpdateAsync(T entity) => Task.CompletedTask;
            public void UpdateWithoutSave(T entity) { }
            public Task DeleteAsync(T entity) { entity.IsDeleted = true; return Task.CompletedTask; }
            public void DeleteWithoutSave(T entity) { entity.IsDeleted = true; }
            public Task HardDeleteAsync(T entity) { Items.Remove(entity); return Task.CompletedTask; }
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
                if (selector == null) throw new ArgumentException("Selector is required", nameof(selector));
                var query = Items.Where(item => !item.IsDeleted);
                if (filter != null)
                {
                    query = query.Where(filter.Compile()).ToList();
                }
                return Task.FromResult<IEnumerable<TResult>>(query.Select(selector.Compile()).ToList());
            }
        }

        private sealed class FakeEnrollmentService : IEnrollmentService
        {
            public Task<EnrollmentDto?> ResetStatusAsync(int enrollmentId) => Task.FromResult<EnrollmentDto?>(null);
            public Task<EnrollmentDto?> GetByIdAsync(int enrollmentId) => Task.FromResult<EnrollmentDto?>(null);
            public Task<EnrollmentDto?> UpdateCompletionAsync(int enrollmentId, bool isComplete) => Task.FromResult<EnrollmentDto?>(null);
            public Task<BulkAssignResultDto> BulkAssignAsync(BulkAssignDto dto) => Task.FromResult(new BulkAssignResultDto { Success = true });
        }

        private sealed class NullNotificationService : INotificationService
        {
            public Task NotifyAsync(string recipientUserId, string type, string level, string title, string? message = null, string? linkPath = null, string? entityType = null, int? entityId = null) => Task.CompletedTask;
            public Task<Application.DTOs.NotificationListDto> GetForUserAsync(string userId, bool unreadOnly, int take, int skip = 0) => Task.FromResult(new Application.DTOs.NotificationListDto());
            public Task<int> GetUnreadCountAsync(string userId) => Task.FromResult(0);
            public Task<int> MarkReadAsync(string userId, int notificationId) => Task.FromResult(0);
            public Task<int> MarkAllReadAsync(string userId) => Task.FromResult(0);
        }
    }
}