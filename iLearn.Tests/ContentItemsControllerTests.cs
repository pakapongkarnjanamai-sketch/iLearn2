using iLearn.API.Controllers;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq.Expressions;

namespace iLearn.Tests
{
    public sealed class ContentItemsControllerTests
    {
        [Fact]
        public async Task GetPaged_ReturnsCorrectPaginationAndFilters()
        {
            // Arrange
            var dataItems = new List<ContentItem>
            {
                new() { Id = 1, Name = "Basic Safety", TypeId = 1, IsActive = true, URL = "scorm/basic-safety" },
                new() { Id = 2, Name = "Advanced SCORM Exam", TypeId = 2, IsActive = false, URL = "scorm/scorm-exam" },
                new() { Id = 3, Name = "Unpublished Course", TypeId = 1, IsActive = false, URL = "scorm/unpublished" }
            };

            var controller = CreateController(new InMemoryGenericRepository<ContentItem>(dataItems));

            // Test case 1: Retrieve page 1 with size 2 (should return first 2 items sorted by ID descending)
            var p1 = new PaginationParams { Page = 1, PageSize = 2 };
            var result1 = await controller.GetPaged(p1);
            var okResult1 = Assert.IsType<OkObjectResult>(result1);
            
            dynamic? value1 = okResult1.Value;
            Assert.NotNull(value1);
            
            var success1 = (bool)value1!.GetType().GetProperty("success").GetValue(value1);
            var totalCount1 = (int)value1!.GetType().GetProperty("totalCount").GetValue(value1);
            var data1 = (List<ContentItemDto>)value1!.GetType().GetProperty("data").GetValue(value1);

            Assert.True(success1);
            Assert.Equal(3, totalCount1);
            Assert.Equal(2, data1.Count);
            // Default sort is descending by ID
            Assert.Equal(3, data1[0].Id);
            Assert.Equal("Unpublished Course", data1[0].Name);
            Assert.Equal(2, data1[1].Id);
            Assert.Equal("Advanced SCORM Exam", data1[1].Name);

            // Test case 2: Search filter "exam"
            var p2 = new PaginationParams { Page = 1, PageSize = 10, Search = "exam" };
            var result2 = await controller.GetPaged(p2);
            var okResult2 = Assert.IsType<OkObjectResult>(result2);
            dynamic? value2 = okResult2.Value;
            var totalCount2 = (int)value2!.GetType().GetProperty("totalCount").GetValue(value2);
            var data2 = (List<ContentItemDto>)value2!.GetType().GetProperty("data").GetValue(value2);

            Assert.Equal(1, totalCount2);
            Assert.Single(data2);
            Assert.Equal("Advanced SCORM Exam", data2[0].Name);

            // Test case 3: Status filter "Published"
            var p3 = new PaginationParams { Page = 1, PageSize = 10, Status = "Published" };
            var result3 = await controller.GetPaged(p3);
            var okResult3 = Assert.IsType<OkObjectResult>(result3);
            dynamic? value3 = okResult3.Value;
            var totalCount3 = (int)value3!.GetType().GetProperty("totalCount").GetValue(value3);
            var data3 = (List<ContentItemDto>)value3!.GetType().GetProperty("data").GetValue(value3);

            Assert.Equal(1, totalCount3);
            Assert.Single(data3);
            Assert.Equal("Basic Safety", data3[0].Name);
        }

        private static ContentItemsController CreateController(IGenericRepository<ContentItem> contentRepository)
        {
            return new ContentItemsController(
                contentRepository,
                new InMemoryGenericRepository<FileStorage>([]),
                new FakeContentPublicationService(),
                new FakeScormService(),
                NullLogger<ContentItemsController>.Instance,
                new FakeMaintenanceStatusService(),
                new FakeAdminActivityService(),
                new MemoryCache(new MemoryCacheOptions()),
                new InMemoryGenericRepository<CourseContentItem>([]),
                new NullNotificationService(),
                new FakeCurrentUserService());
        }

        private sealed class FakeContentPublicationService : IContentPublicationService
        {
            public Task<ContentItemDto> PublishAsync(int contentItemId) => throw new NotImplementedException();
            public Task<ContentItemDto> UnpublishAsync(int contentItemId) => throw new NotImplementedException();
            public Task<ContentUnpublishImpactPreviewDto> PreviewBatchUnpublishAsync(IEnumerable<int> contentItemIds) => throw new NotImplementedException();
        }

        private sealed class FakeMaintenanceStatusService : IMaintenanceStatusService
        {
            public Guid BeginOperation(string operationName, int totalItems, string initiatedBy) => Guid.NewGuid();
            public void UpdateOperation(Guid operationId, string currentStep, string? currentItemName = null, int? currentItem = null, int? successCount = null, int? failureCount = null) { }
            public void CompleteOperation(Guid operationId, bool isSuccess, string completedStep, int successCount, int failureCount) { }
            public IReadOnlyCollection<MaintenanceOperationStatus> GetActiveOperations() => Array.Empty<MaintenanceOperationStatus>();
        }

        private sealed class FakeAdminActivityService : IAdminActivityService
        {
            public Task LogAsync(string actionType, string entityType, int? entityId, string title, string? description = null, int? divisionId = null, string? dataJson = null) => Task.CompletedTask;
            public Task<IReadOnlyList<AdminActivityDto>> GetRecentActivitiesAsync(int take = 20, int? divisionId = null) => throw new NotImplementedException();
        }

        private sealed class FakeScormService : IScormService
        {
            public Task<ScormManifestDto> ExtractAndParseScormAsync(byte[] fileContent, string folderName) => throw new NotImplementedException();
            public Task<ScormManifestDto> ExtractAndParseScormFromFileAsync(string zipFilePath, string folderName) => throw new NotImplementedException();
            public Task<string> SavePackageToArchiveAsync(Stream stream, string archiveFileName) => Task.FromResult($"Courses/_archives/{archiveFileName}");
            public void DeleteScormFolder(string folderName) { }
            public void DeleteArchiveFile(string storagePath) { }
            public string GetArchiveFullPath(string relativePath) => relativePath;
            public string GetScormUrl(string folderName, string launchHref) => $"{folderName}/{launchHref}";
            public (int FileCount, long TotalSize) GetFolderInfo(string folderName) => (0, 0);
        }

        private sealed class InMemoryGenericRepository<T> : IGenericRepository<T> where T : BaseEntity
        {
            public InMemoryGenericRepository(IEnumerable<T> items)
            {
                Items = items.ToList();
            }

            public List<T> Items { get; }

            public Task<IReadOnlyList<T>> GetAllAsync() => Task.FromResult<IReadOnlyList<T>>(Items.ToList());
            public Task<T?> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(item => item.Id == id));
            public Task<T> AddAsync(T entity) => throw new NotSupportedException();
            public Task<T> AddWithoutSaveAsync(T entity) => throw new NotSupportedException();
            public Task UpdateAsync(T entity) => throw new NotSupportedException();
            public void UpdateWithoutSave(T entity) => throw new NotSupportedException();
            public Task DeleteAsync(T entity) => throw new NotSupportedException();
            public void DeleteWithoutSave(T entity) => throw new NotSupportedException();
            public Task HardDeleteAsync(T entity) => throw new NotSupportedException();
            public IQueryable<T> GetQuery() => Items.AsQueryable();

            public Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, bool ignoreQueryFilters = false)
            {
                IQueryable<T> query = Items.AsQueryable();
                if (filter != null)
                {
                    query = query.Where(filter.Compile()).AsQueryable();
                }
                return Task.FromResult<IReadOnlyList<T>>(query.ToList());
            }

            public Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
            {
                IQueryable<T> query = Items.AsQueryable();
                if (filter != null)
                {
                    query = query.Where(filter.Compile()).AsQueryable();
                }
                return Task.FromResult(query.Count());
            }

            public Task<IEnumerable<TResult>> GetAsync<TResult>(Expression<Func<T, bool>>? filter = null, Expression<Func<T, TResult>>? selector = null)
            {
                IQueryable<T> query = Items.AsQueryable();
                if (filter != null)
                {
                    query = query.Where(filter.Compile()).AsQueryable();
                }
                if (selector == null)
                {
                    return Task.FromResult(Enumerable.Empty<TResult>());
                }
                return Task.FromResult(query.Select(selector.Compile()));
            }
        }

        private sealed class NullNotificationService : INotificationService
        {
            public Task NotifyAsync(string recipientUserId, string type, string level, string title, string? message = null, string? linkPath = null, string? entityType = null, int? entityId = null) => Task.CompletedTask;
            public Task<Application.DTOs.NotificationListDto> GetForUserAsync(string userId, bool unreadOnly, int take) => Task.FromResult(new Application.DTOs.NotificationListDto());
            public Task<int> GetUnreadCountAsync(string userId) => Task.FromResult(0);
            public Task<int> MarkReadAsync(string userId, int notificationId) => Task.FromResult(0);
            public Task<int> MarkAllReadAsync(string userId) => Task.FromResult(0);
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public string UserId => "testuser";
            public string FullName => "TEST\\testuser";
            public bool IsAuthenticated => true;
            public int? DivisionId => null;
            public string? DivisionName => null;
            public bool IsSuperAdmin => true;
        }
    }
}
