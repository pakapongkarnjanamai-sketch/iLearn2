using DevExtreme.AspNet.Mvc;
using iLearn.API.Controllers.Base;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Linq.Expressions;

namespace iLearn.Tests
{
    public sealed class CoursesCrudControllerTests
    {
        [Fact]
        public async Task Get_ReturnsCourseDetailDtoWithLifecycleSemantics()
        {
            var expected = new CourseDetailDto
            {
                Id = 5,
                CourseCode = "C-05",
                CourseName = "Safety Course",
                Description = "Required annual training",
                CourseType = 4,
                CategoryId = 2,
                IsActive = true,
                Status = CourseStatus.Closed,
                CanAssign = false,
                CanLearnerAccess = true,
                ContentItems =
                [
                    new CourseContentItemDto
                    {
                        Id = 100,
                        Name = "Exam",
                        TypeId = 2,
                        TypeName = "Exam",
                        IsActive = true,
                        URL = "pkg://exam"
                    }
                ]
            };

            var controller = new CoursesCRUDController(
                new InMemoryGenericRepository<Course>([]),
                new FakeCurrentUserService(),
                new FakeCourseService(expected));

            var result = await controller.Get(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<CourseDetailDto>(ok.Value);

            Assert.Equal(5, dto.Id);
            Assert.Equal("C-05", dto.CourseCode);
            Assert.Equal("Safety Course", dto.CourseName);
            Assert.Equal("Closed", dto.StatusName);
            Assert.False(dto.CanAssign);
            Assert.True(dto.CanLearnerAccess);
            var contentItem = Assert.Single(dto.ContentItems);
            Assert.Equal("Exam", contentItem.TypeName);
            Assert.True(contentItem.IsPublished);
            Assert.Equal("Published", contentItem.PublishState);
        }

        [Fact]
        public async Task Get_UnknownId_ReturnsNotFound()
        {
            var controller = new CoursesCRUDController(
                new InMemoryGenericRepository<Course>([]),
                new FakeCurrentUserService(),
                new FakeCourseService(null));

            var result = await controller.Get(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetActive_ReturnsSemanticProjectedRows()
        {
            var readyCourse = new Course
            {
                Id = 9,
                Code = "C-09",
                Title = "Open Ready Course",
                Status = CourseStatus.Open,
                IsActive = true,
                CategoryId = 2,
                Category = new Category { Id = 2, Name = "Safety", DivisionId = 7 },
                CourseTypeId = 4,
                CourseType = new CourseType { Id = 4, Name = "Common" },
                Versions =
                [
                    new CourseVersion
                    {
                        Id = 90,
                        CourseId = 9,
                        IsActive = true,
                        CourseContentItems =
                        [
                            new CourseContentItem
                            {
                                Id = 1,
                                ContentItemId = 100,
                                ContentItem = new ContentItem
                                {
                                    Id = 100,
                                    Name = "Ready Learn",
                                    TypeId = 1,
                                    IsActive = true,
                                    URL = "scorm/pkg-100",
                                    LaunchHref = "launch/index.html"
                                }
                            }
                        ]
                    }
                ]
            };

            var draftCourse = new Course
            {
                Id = 10,
                Code = "C-10",
                Title = "Draft Course",
                Status = CourseStatus.Draft,
                CategoryId = 2,
                Category = new Category { Id = 2, Name = "Safety", DivisionId = 7 },
                CourseTypeId = 4,
                CourseType = new CourseType { Id = 4, Name = "Common" }
            };

            var controller = new CoursesCRUDController(
                new InMemoryGenericRepository<Course>([readyCourse, draftCourse]),
                new FakeCurrentUserService(),
                new FakeCourseService(null));

            var result = await controller.GetActive(new DataSourceLoadOptions());

            var ok = Assert.IsType<OkObjectResult>(result);
            var loadResult = ok.Value!;
            var dataProperty = loadResult.GetType().GetProperty("data");
            Assert.NotNull(dataProperty);

            var rows = Assert.IsAssignableFrom<IEnumerable>(dataProperty!.GetValue(loadResult));
            var row = Assert.Single(rows.Cast<object>());

            Assert.Equal("C-09", row.GetType().GetProperty("Code")?.GetValue(row)?.ToString());
            Assert.Equal("Open", row.GetType().GetProperty("StatusName")?.GetValue(row)?.ToString());
            Assert.Equal("Safety", row.GetType().GetProperty("CategoryName")?.GetValue(row)?.ToString());
            Assert.Equal("Common", row.GetType().GetProperty("CourseTypeName")?.GetValue(row)?.ToString());
            Assert.Equal(true, row.GetType().GetProperty("CanAssign")?.GetValue(row));
            Assert.Equal(true, row.GetType().GetProperty("CanLearnerAccess")?.GetValue(row));
        }

        private sealed class FakeCourseService : ICourseService
        {
            private readonly CourseDetailDto? _course;

            public FakeCourseService(CourseDetailDto? course)
            {
                _course = course;
            }

            public Task<IEnumerable<CourseDto>> GetAllCoursesAsync(bool isActive = true) => throw new NotSupportedException();
            public Task<IEnumerable<CourseDto>> GetCoursesByDivisionNameAsync(string divisionName, bool isActive = true) => throw new NotSupportedException();
            public Task<CourseDetailDto> GetCourseByIdAsync(int id) => Task.FromResult(_course)!;
            public Task<CourseDto> CreateCourseAsync(CourseCreateDto model) => throw new NotSupportedException();
            public Task<CourseDto> CreateCourseWithScormAsync(CourseCreateDto model) => throw new NotSupportedException();
            public Task<CourseDto> UpdateCourseAsync(int id, CourseCreateDto model) => throw new NotSupportedException();
            public Task<bool> UpdateCourseStatusAsync(int id, bool isActive) => throw new NotSupportedException();
            public Task DeleteCourseAsync(int id, bool force = false) => throw new NotSupportedException();
            public Task<CourseStatusResultDto> UpdateCourseStatusAsync(int id, CourseStatus status) => throw new NotSupportedException();
            public Task<CourseStatusImpactDto> GetCourseStatusImpactAsync(int id) => throw new NotSupportedException();
            public Task<List<CourseLearnerDto>> GetCourseLearnersAsync(int courseId) => throw new NotSupportedException();
            public Task<List<CourseAssignmentHistoryDto>> GetCourseAssignmentsAsync(int courseId) => throw new NotSupportedException();
            public Task<CourseDashboardDto> GetCourseDashboardAsync(int courseId) => throw new NotSupportedException();
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