using System.Linq.Expressions;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;

namespace iLearn.Tests
{
    public class ContentPublicationServiceTests
    {
        [Fact]
        public async Task UnpublishAsync_RejectsContentLinkedToCourseVersions()
        {
            var contentRepo = new InMemoryRepository<ContentItem>(new ContentItem
            {
                Id = 10,
                Name = "course.zip",
                IsActive = true,
                URL = "pkg-10",
                LaunchHref = "index.html",
                SchemaVersion = "1.2",
                CourseContentItems = new List<CourseContentItem>
                {
                    new() { Id = 1, ContentItemId = 10, CourseVersionId = 100 }
                }
            });
            var fileRepo = new InMemoryRepository<FileStorage>();
            var scormService = new FakeScormService();
            var service = new ContentPublicationService(contentRepo, fileRepo, scormService);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UnpublishAsync(10));

            Assert.Equal("Cannot unpublish a content item that is used by courses. Remove it from all course versions first.", exception.Message);
            Assert.Empty(scormService.DeletedFolders);
            Assert.True(contentRepo.Items.Single().IsActive);
        }

        [Fact]
        public async Task UnpublishAsync_ClearsPublishFieldsAndDeletesFolder()
        {
            var contentRepo = new InMemoryRepository<ContentItem>(new ContentItem
            {
                Id = 11,
                Name = "course.zip",
                IsActive = true,
                URL = "pkg-11",
                LaunchHref = "launch.html",
                SchemaVersion = "2004"
            });
            var fileRepo = new InMemoryRepository<FileStorage>();
            var scormService = new FakeScormService();
            var service = new ContentPublicationService(contentRepo, fileRepo, scormService);

            var dto = await service.UnpublishAsync(11);

            var stored = contentRepo.Items.Single();
            Assert.False(stored.IsActive);
            Assert.Null(stored.URL);
            Assert.Null(stored.LaunchHref);
            Assert.Null(stored.SchemaVersion);
            Assert.Equal(new[] { "pkg-11" }, scormService.DeletedFolders);
            Assert.False(dto.IsPublished);
            Assert.Equal("Unpublished", dto.PublishState);
        }

        [Fact]
        public async Task PublishAsync_SetsSemanticPublishStateForZipContent()
        {
            var contentRepo = new InMemoryRepository<ContentItem>(new ContentItem
            {
                Id = 12,
                Name = "course.zip",
                IsActive = false,
                FileStorageId = 99
            });
            var fileRepo = new InMemoryRepository<FileStorage>(new FileStorage
            {
                Id = 99,
                Name = "course.zip",
                Data = new byte[] { 1, 2, 3 },
                Length = 3
            });
            var scormService = new FakeScormService();
            var service = new ContentPublicationService(contentRepo, fileRepo, scormService);

            var dto = await service.PublishAsync(12);

            var stored = contentRepo.Items.Single();
            Assert.True(stored.IsActive);
            Assert.Equal("launch/index.html", stored.LaunchHref);
            Assert.Equal("SCORM 2004", stored.SchemaVersion);
            Assert.Equal("published-folder", stored.URL);
            Assert.True(dto.IsPublished);
            Assert.Equal("Published", dto.PublishState);
        }

        [Fact]
        public async Task PreviewBatchUnpublishAsync_SeparatesEligibleAndBlockedItems()
        {
            var contentRepo = new InMemoryRepository<ContentItem>(
                new ContentItem
                {
                    Id = 20,
                    Name = "unused.zip",
                    IsActive = true,
                    URL = "pkg-20"
                },
                new ContentItem
                {
                    Id = 21,
                    Name = "linked.zip",
                    IsActive = true,
                    URL = "pkg-21",
                    CourseContentItems = new List<CourseContentItem>
                    {
                        new()
                        {
                            Id = 1,
                            ContentItemId = 21,
                            CourseVersionId = 200,
                            CourseVersion = new CourseVersion
                            {
                                Id = 200,
                                CourseId = 7,
                                VersionNumber = 3,
                                Course = new Course
                                {
                                    Id = 7,
                                    Code = "SAFE-101"
                                }
                            }
                        }
                    }
                });
            var fileRepo = new InMemoryRepository<FileStorage>();
            var scormService = new FakeScormService();
            var service = new ContentPublicationService(contentRepo, fileRepo, scormService);

            var preview = await service.PreviewBatchUnpublishAsync([20, 21]);

            Assert.Equal(2, preview.RequestedCount);
            Assert.Equal(1, preview.EligibleCount);
            Assert.Equal(1, preview.BlockedCount);
            Assert.Equal([20], preview.EligibleIds);
            Assert.Collection(preview.Items.OrderBy(item => item.ContentItemId),
                eligible =>
                {
                    Assert.Equal(20, eligible.ContentItemId);
                    Assert.True(eligible.CanUnpublish);
                    Assert.Null(eligible.BlockingReason);
                },
                blocked =>
                {
                    Assert.Equal(21, blocked.ContentItemId);
                    Assert.False(blocked.CanUnpublish);
                    Assert.Equal("Content item is used by course versions and must be removed from those versions first.", blocked.BlockingReason);
                    Assert.Equal(["SAFE-101"], blocked.LinkedCourseCodes);
                });
        }

        private sealed class FakeScormService : IScormService
        {
            public List<string> DeletedFolders { get; } = new();

            public Task<ScormManifestDto> ExtractAndParseScormAsync(byte[] fileContent, string folderName)
            {
                return Task.FromResult(new ScormManifestDto
                {
                    FolderName = "published-folder",
                    LaunchHref = "launch/index.html",
                    SchemaVersion = "SCORM 2004"
                });
            }

            public void DeleteScormFolder(string folderName)
            {
                DeletedFolders.Add(folderName);
            }

            public (int FileCount, long TotalSize) GetFolderInfo(string folderName) => (0, 0);

            public string GetScormUrl(string folderName, string launchHref) => $"https://files.example.local/{folderName}/{launchHref}";
        }

        private sealed class InMemoryRepository<T> : IGenericRepository<T> where T : BaseEntity
        {
            public InMemoryRepository(params T[] items)
            {
                Items = items.ToList();
            }

            public List<T> Items { get; }

            public Task<IReadOnlyList<T>> GetAllAsync() => Task.FromResult<IReadOnlyList<T>>(Items);

            public Task<T?> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

            public Task<T> AddAsync(T entity)
            {
                Items.Add(entity);
                return Task.FromResult(entity);
            }

            public Task<T> AddWithoutSaveAsync(T entity)
            {
                Items.Add(entity);
                return Task.FromResult(entity);
            }

            public Task UpdateAsync(T entity) => Task.CompletedTask;

            public void UpdateWithoutSave(T entity)
            {
            }

            public Task DeleteAsync(T entity)
            {
                entity.IsDeleted = true;
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

            public IQueryable<T> GetQuery() => Items.AsQueryable();

            public Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, bool ignoreQueryFilters = false)
            {
                IQueryable<T> query = Items.AsQueryable();
                if (filter != null)
                {
                    query = query.Where(filter);
                }

                return Task.FromResult<IReadOnlyList<T>>(query.ToList());
            }

            public Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
            {
                IQueryable<T> query = Items.AsQueryable();
                if (filter != null)
                {
                    query = query.Where(filter);
                }

                return Task.FromResult(query.Count());
            }

            public Task<IEnumerable<TResult>> GetAsync<TResult>(Expression<Func<T, bool>>? filter = null, Expression<Func<T, TResult>>? selector = null)
            {
                IQueryable<T> query = Items.AsQueryable();
                if (filter != null)
                {
                    query = query.Where(filter);
                }

                if (selector == null)
                {
                    return Task.FromResult(Enumerable.Empty<TResult>());
                }

                return Task.FromResult(query.Select(selector).AsEnumerable());
            }
        }
    }
}