using iLearn.API.Controllers.Base;
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
    public sealed class ContentItemsCrudControllerTests
    {
        [Fact]
        public async Task Get_ReturnsContentItemDtoWithSemanticLifecycleFields()
        {
            var controller = CreateController(
                new InMemoryGenericRepository<ContentItem>(
                [
                    new ContentItem
                    {
                        Id = 15,
                        Name = "Safety Exam",
                        TypeId = 2,
                        IsActive = true,
                        URL = "pkg://safety-exam",
                        FileStorageId = 44
                    }
                ]));

            var result = await controller.Get(15);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<ContentItemDto>(ok.Value);

            Assert.Equal(15, dto.Id);
            Assert.Equal("Safety Exam", dto.Name);
            Assert.Equal(2, dto.TypeId);
            Assert.True(dto.IsActive);
            Assert.True(dto.IsPublished);
            Assert.Equal("Published", dto.PublishState);
            Assert.Equal("/api/contentItems/15/content", dto.ContentUrl);
        }

        [Fact]
        public async Task Get_UnknownId_ReturnsNotFound()
        {
            var controller = CreateController(new InMemoryGenericRepository<ContentItem>([]));

            var result = await controller.Get(999);

            Assert.IsType<NotFoundResult>(result);
        }

        private static ContentItemsCRUDController CreateController(IGenericRepository<ContentItem> contentRepository)
        {
            return new ContentItemsCRUDController(
                contentRepository,
                new FakeCurrentUserService(),
                new InMemoryGenericRepository<CourseContentItem>([]),
                new InMemoryGenericRepository<Course>([]),
                new InMemoryGenericRepository<FileStorage>([]),
                new FakeScormService(),
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<ContentItemsCRUDController>.Instance);
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

        private sealed class FakeScormService : IScormService
        {
            public Task<ScormManifestDto> ExtractAndParseScormAsync(byte[] fileContent, string folderName) => throw new NotSupportedException();

            public void DeleteScormFolder(string folderName)
            {
            }

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
    }
}