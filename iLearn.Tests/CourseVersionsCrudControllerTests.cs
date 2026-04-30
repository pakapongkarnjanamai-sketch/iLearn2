using iLearn.API.Controllers.Base;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace iLearn.Tests
{
    public sealed class CourseVersionsCrudControllerTests
    {
        private static readonly DateTime Now = new(2026, 4, 30, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Get_ReturnsCourseVersionDtoWithSemanticLifecycleFields()
        {
            var repository = new InMemoryGenericRepository<CourseVersion>(
            [
                new CourseVersion
                {
                    Id = 7,
                    CourseId = 3,
                    VersionNumber = 2,
                    Note = "Release 2",
                    IsActive = true,
                    CreatedAt = Now,
                    CourseContentItems =
                    [
                        new CourseContentItem
                        {
                            Id = 1,
                            Order = 2,
                            ContentItemId = 100,
                            ContentItem = new ContentItem
                            {
                                Id = 100,
                                Name = "Exam A",
                                TypeId = 2,
                                IsActive = true,
                                URL = "pkg://exam-a"
                            }
                        },
                        new CourseContentItem
                        {
                            Id = 2,
                            Order = 1,
                            ContentItemId = 101,
                            ContentItem = new ContentItem
                            {
                                Id = 101,
                                Name = "Learn B",
                                TypeId = 1,
                                IsActive = false,
                                URL = "pkg://learn-b"
                            }
                        }
                    ]
                }
            ]);

            var controller = new CourseVersionsCRUDController(repository, new FakeCurrentUserService());

            var result = await controller.Get(7);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<CourseVersionDto>(ok.Value);

            Assert.Equal(7, dto.Id);
            Assert.Equal(3, dto.CourseId);
            Assert.Equal(2, dto.VersionNumber);
            Assert.Equal("Release 2", dto.Note);
            Assert.True(dto.IsActive);
            Assert.Equal("Active", dto.VersionState);
            Assert.Equal(Now, dto.CreatedAt);
            Assert.Collection(dto.ContentItems,
                first =>
                {
                    Assert.Equal(101, first.Id);
                    Assert.Equal("Learn B", first.Name);
                    Assert.Equal(1, first.TypeId);
                    Assert.Equal("Learn", first.TypeName);
                    Assert.False(first.IsPublished);
                    Assert.Equal("Unpublished", first.PublishState);
                    Assert.Equal("pkg://learn-b", first.URL);
                },
                second =>
                {
                    Assert.Equal(100, second.Id);
                    Assert.Equal("Exam A", second.Name);
                    Assert.Equal(2, second.TypeId);
                    Assert.Equal("Exam", second.TypeName);
                    Assert.True(second.IsPublished);
                    Assert.Equal("Published", second.PublishState);
                    Assert.Equal("pkg://exam-a", second.URL);
                });
        }

        [Fact]
        public async Task Get_UnknownId_ReturnsNotFound()
        {
            var controller = new CourseVersionsCRUDController(
                new InMemoryGenericRepository<CourseVersion>([]),
                new FakeCurrentUserService());

            var result = await controller.Get(999);

            Assert.IsType<NotFoundResult>(result);
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