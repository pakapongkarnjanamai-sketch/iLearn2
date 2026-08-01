using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using iLearn.API.Controllers;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using Xunit;
using iLearn.Application.DTOs;

namespace iLearn.Tests
{
    public sealed class LearnersControllerTests
    {
        [Fact]
        public async Task Get_MapsCamelCaseFilterFieldsToPascalCase()
        {
            // Arrange
            var capturedQueryString = "";
            var fakeLearnerService = new FakeLearnerApiService
            {
                GetLearnersDxGridAsyncHandler = (qs) =>
                {
                    capturedQueryString = qs;
                    return Task.FromResult("{\"data\": [], \"totalCount\": 0}");
                }
            };

            var fakeCurrentUser = new FakeCurrentUserService();

            var controller = new LearnersController(
                fakeLearnerService,
                null!,
                null!,
                fakeCurrentUser)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // query: filter=[["nid","contains","61"],"or",["englishFirstName","contains","61"],"or",["englishLastName","contains","61"],"or",["eId","contains","61"]]
            controller.HttpContext.Request.QueryString = new QueryString("?skip=0&take=19&requireTotalCount=true&filter=%5B%5B%22nid%22%2C%22contains%22%2C%2261%22%5D%2C%22or%22%2C%5B%22englishFirstName%22%2C%22contains%22%2C%2261%22%5D%2C%22or%22%2C%5B%22englishLastName%22%2C%22contains%22%2C%2261%22%5D%2C%22or%22%2C%5B%22eId%22%2C%22contains%22%2C%2261%22%5D%5D");

            // Act
            var result = await controller.Get();

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Contains("filter=", capturedQueryString);
            
            // Extract the filter part and unescape it
            var match = System.Text.RegularExpressions.Regex.Match(capturedQueryString, @"([?&])filter=([^&]*)");
            Assert.True(match.Success);
            var decodedFilter = Uri.UnescapeDataString(match.Groups[2].Value);

            // The fields should be mapped to PascalCase
            Assert.Contains("\"NID\"", decodedFilter);
            Assert.Contains("\"EnglishFirstName\"", decodedFilter);
            Assert.Contains("\"EnglishLastName\"", decodedFilter);
            Assert.Contains("\"EId\"", decodedFilter);

            // Verify camelCase fields are NOT present
            Assert.DoesNotContain("\"nid\"", decodedFilter);
            Assert.DoesNotContain("\"englishFirstName\"", decodedFilter);
            Assert.DoesNotContain("\"englishLastName\"", decodedFilter);
            Assert.DoesNotContain("\"eId\"", decodedFilter);
        }

        [Fact]
        public async Task Get_ProtectsValuesFromBeingMapped()
        {
            // Arrange
            var capturedQueryString = "";
            var fakeLearnerService = new FakeLearnerApiService
            {
                GetLearnersDxGridAsyncHandler = (qs) =>
                {
                    capturedQueryString = qs;
                    return Task.FromResult("{\"data\": [], \"totalCount\": 0}");
                }
            };

            var fakeCurrentUser = new FakeCurrentUserService();

            var controller = new LearnersController(
                fakeLearnerService,
                null!,
                null!,
                fakeCurrentUser)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // query: filter=["englishFirstName","contains","nid"]
            // we search for the value "nid". The value "nid" should remain "nid" (lowercase), but field name "englishFirstName" should become "EnglishFirstName"
            controller.HttpContext.Request.QueryString = new QueryString("?filter=%5B%22englishFirstName%22%2C%22contains%22%2C%22nid%22%5D");

            // Act
            var result = await controller.Get();

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var match = System.Text.RegularExpressions.Regex.Match(capturedQueryString, @"([?&])filter=([^&]*)");
            Assert.True(match.Success);
            var decodedFilter = Uri.UnescapeDataString(match.Groups[2].Value);

            Assert.Contains("\"EnglishFirstName\"", decodedFilter);
            Assert.Contains("\"nid\"", decodedFilter); // Value should NOT be modified
        }

        [Fact]
        public async Task Get_InjectsDivisionFilterAndMapsCasing()
        {
            // Arrange
            var capturedQueryString = "";
            var fakeLearnerService = new FakeLearnerApiService
            {
                GetLearnersDxGridAsyncHandler = (qs) =>
                {
                    capturedQueryString = qs;
                    return Task.FromResult("{\"data\": [], \"totalCount\": 0}");
                }
            };

            var fakeCurrentUser = new FakeCurrentUserService
            {
                DivisionId = 12,
                DivisionName = "HR Department"
            };

            var controller = new LearnersController(
                fakeLearnerService,
                null!,
                null!,
                fakeCurrentUser)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // query: filter=["nid","=","123"]
            controller.HttpContext.Request.QueryString = new QueryString("?filter=%5B%22nid%22%2C%22%3D%22%2C%22123%22%5D");

            // Act
            var result = await controller.Get();

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var match = System.Text.RegularExpressions.Regex.Match(capturedQueryString, @"([?&])filter=([^&]*)");
            Assert.True(match.Success);
            var decodedFilter = Uri.UnescapeDataString(match.Groups[2].Value);

            // NID should be mapped to PascalCase
            Assert.Contains("\"NID\"", decodedFilter);
            // Division filter should be injected with correct value
            Assert.Contains("\"Division\"", decodedFilter);
            Assert.Contains("\"HR Department\"", decodedFilter);
        }

        [Fact]
        public async Task GetProfile_DeletedOnlyAssignmentLink_IsCancelledNotActive()
        {
            var course = new Course { Id = 10, Code = "C-10", Title = "Course 10" };
            var deletedAssignment = new Assignment { Id = 20, AssignmentNo = "ASG-DEL", IsDeleted = true };
            var enrollment = new Enrollment
            {
                Id = 30,
                LearnerCode = "EMP001",
                CourseId = course.Id,
                Course = course,
                StartDate = new DateTime(2026, 7, 1),
                DueDate = new DateTime(2026, 7, 31),
                Progress = 10,
            };
            enrollment.AssignmentLinks.Add(new EnrollmentAssignment
            {
                Id = 40,
                EnrollmentId = enrollment.Id,
                Enrollment = enrollment,
                AssignmentId = deletedAssignment.Id,
                Assignment = deletedAssignment,
                IsDeleted = true,
            });

            var controller = new LearnersController(
                new FakeLearnerApiService
                {
                    GetLearnerByCodeAsyncHandler = _ => Task.FromResult(new ExternalLearnerDto
                    {
                        Code = "EMP001",
                        Name = "Learner One",
                        Division = "QA"
                    })
                },
                new InMemoryGenericRepository<Enrollment>([enrollment]),
                null!,
                new FakeCurrentUserService());

            var result = await controller.GetProfile("EMP001");

            var ok = Assert.IsType<OkObjectResult>(result);
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
            var historyRow = json.RootElement.GetProperty("data").GetProperty("enrollments")[0];
            Assert.False(historyRow.GetProperty("hasActiveAssignment").GetBoolean());
            Assert.True(historyRow.GetProperty("isAssignmentCancelled").GetBoolean());
        }

        // Self-enroll / legacy enrollment: ไม่เคยมี assignment link เลย แม้จะมี StartDate/DueDate
        // และยังไม่จบ ก็ไม่ใช่ "assignment ถูกยกเลิก" — UI ต้องขึ้น badge Self Enroll ไม่ใช่ Cancelled
        // (PLAN-185 fix, regression test เพิ่มใน PLAN-187)
        [Fact]
        public async Task GetProfile_EnrollmentWithoutAssignmentLinks_IsNeitherActiveNorCancelled()
        {
            var course = new Course { Id = 11, Code = "C-11", Title = "Course 11" };
            var enrollment = new Enrollment
            {
                Id = 31,
                LearnerCode = "EMP002",
                CourseId = course.Id,
                Course = course,
                StartDate = new DateTime(2026, 7, 1),
                DueDate = new DateTime(2026, 7, 31),
                Progress = 40,
            };

            var controller = new LearnersController(
                new FakeLearnerApiService
                {
                    GetLearnerByCodeAsyncHandler = _ => Task.FromResult(new ExternalLearnerDto
                    {
                        Code = "EMP002",
                        Name = "Learner Two",
                        Division = "QA"
                    })
                },
                new InMemoryGenericRepository<Enrollment>([enrollment]),
                null!,
                new FakeCurrentUserService());

            var result = await controller.GetProfile("EMP002");

            var ok = Assert.IsType<OkObjectResult>(result);
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
            var historyRow = json.RootElement.GetProperty("data").GetProperty("enrollments")[0];
            Assert.False(historyRow.GetProperty("hasActiveAssignment").GetBoolean());
            Assert.False(historyRow.GetProperty("isAssignmentCancelled").GetBoolean());
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public string UserId => "1";
            public string FullName => "Tester Admin";
            public bool IsAuthenticated => true;
            public int? DivisionId { get; set; }
            public string? DivisionName { get; set; }
            public bool IsSuperAdmin => true;
        }

        private sealed class FakeLearnerApiService : ILearnerApiService
        {
            public Func<string, Task<string>>? GetLearnersDxGridAsyncHandler { get; set; }
            public Func<string, Task<ExternalLearnerDto>>? GetLearnerByCodeAsyncHandler { get; set; }

            public Task<string> GetLearnersDxGridAsync(string queryString)
            {
                return GetLearnersDxGridAsyncHandler != null 
                    ? GetLearnersDxGridAsyncHandler(queryString) 
                    : Task.FromResult("{\"data\": [], \"totalCount\": 0}");
            }

            public Task<ExternalLearnerDto> GetLearnerByCodeAsync(string Code) => GetLearnerByCodeAsyncHandler != null
                ? GetLearnerByCodeAsyncHandler(Code)
                : throw new NotImplementedException();
            public Task<AllLearnersApiResponse> GetLearnerAsync() => throw new NotImplementedException();
            public Task<DivisionApiResponse> GetLearnersByDivisionsAsync(string[] divisions, int skip = 0, int take = 20) => throw new NotImplementedException();
            public Task<object> GetSectionsAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetDivisionsAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetDepartmentsAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetPositionsAsync(string queryString) => throw new NotImplementedException();
            public Task<Dictionary<string, ExternalLearnerDto>> GetLearnersByCodesAsync(IEnumerable<string> codes) => throw new NotImplementedException();
            public Task<Dictionary<string, EmployeeCsvDto>> GetEmployeesByNidsAsync(IEnumerable<string> nids) => throw new NotImplementedException();
        }

        private sealed class InMemoryGenericRepository<T> : IGenericRepository<T> where T : BaseEntity
        {
            private readonly List<T> _items;

            public InMemoryGenericRepository(IEnumerable<T> items)
            {
                _items = items.ToList();
            }

            public Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, bool ignoreQueryFilters = false)
            {
                var query = ignoreQueryFilters ? _items.AsEnumerable() : _items.Where(item => !item.IsDeleted);
                if (filter != null)
                {
                    query = query.Where(filter.Compile());
                }

                return Task.FromResult<IReadOnlyList<T>>(query.ToList());
            }

            public Task<IReadOnlyList<T>> GetAllAsync() => throw new NotImplementedException();
            public Task<T?> GetByIdAsync(int id) => throw new NotImplementedException();
            public Task<T> AddAsync(T entity) => throw new NotImplementedException();
            public Task<T> AddWithoutSaveAsync(T entity) => throw new NotImplementedException();
            public Task UpdateAsync(T entity) => throw new NotImplementedException();
            public void UpdateWithoutSave(T entity) => throw new NotImplementedException();
            public Task DeleteAsync(T entity) => throw new NotImplementedException();
            public void DeleteWithoutSave(T entity) => throw new NotImplementedException();
            public Task HardDeleteAsync(T entity) => throw new NotImplementedException();
            public IQueryable<T> GetQuery() => throw new NotImplementedException();
            public Task<int> CountAsync(Expression<Func<T, bool>>? filter = null) => throw new NotImplementedException();
            public Task<IEnumerable<TResult>> GetAsync<TResult>(Expression<Func<T, bool>>? filter = null, Expression<Func<T, TResult>>? selector = null) => throw new NotImplementedException();
        }

        [Fact]
        public void MapFilterFieldNames_FormEncodedPlus_DecodesToSpaceAndMapsField()
        {
            // query: filter=["section","=","Corporate+Support+Division+(FM)"]
            var queryString = "?filter=%5B%22section%22%2C%22%3D%22%2C%22Corporate+Support+Division+%28FM%29%22%5D";
            
            var result = LearnersController.MapFilterFieldNames(queryString);
            
            var match = System.Text.RegularExpressions.Regex.Match(result, @"filter=([^&]*)");
            Assert.True(match.Success);
            var decoded = Uri.UnescapeDataString(match.Groups[1].Value);
            
            // Expected: ["Section","=","Corporate Support Division (FM)"]
            Assert.Contains("\"Section\"", decoded);
            Assert.Contains("Corporate Support Division (FM)", decoded);
            Assert.DoesNotContain("+", decoded);
        }

        [Fact]
        public void MapFilterFieldNames_Percent20EncodedSpace_KeepsSpacesAndMapsField()
        {
            // query: filter=["section","=","Corporate%20Support%20Division%20(FM)"]
            var queryString = "?filter=%5B%22section%22%2C%22%3D%22%2C%22Corporate%20Support%20Division%20%28FM%29%22%5D";
            
            var result = LearnersController.MapFilterFieldNames(queryString);
            
            var match = System.Text.RegularExpressions.Regex.Match(result, @"filter=([^&]*)");
            Assert.True(match.Success);
            var decoded = Uri.UnescapeDataString(match.Groups[1].Value);
            
            // Expected: ["Section","=","Corporate Support Division (FM)"]
            Assert.Contains("\"Section\"", decoded);
            Assert.Contains("Corporate Support Division (FM)", decoded);
        }

        [Fact]
        public void MapFilterFieldNames_Percent2BEncodedPlus_PreservesPlusLiteral()
        {
            // query: filter=["position","=","M1+"]
            // %2B is the encoding for +
            var queryString = "?filter=%5B%22position%22%2C%22%3D%22%2C%22M1%2B%22%5D";
            
            var result = LearnersController.MapFilterFieldNames(queryString);
            
            var match = System.Text.RegularExpressions.Regex.Match(result, @"filter=([^&]*)");
            Assert.True(match.Success);
            var decoded = Uri.UnescapeDataString(match.Groups[1].Value);
            
            // Expected: ["Position","=","M1+"]
            Assert.Contains("\"Position\"", decoded);
            Assert.Contains("M1+", decoded);
        }

        [Fact]
        public void MapFilterFieldNames_ThaiNameFields_MapsToPascalCase()
        {
            // query: filter=[["thaiFirstName","contains","สม"],"or",["thaiLastName","contains","สม"]]
            var queryString = "?filter=%5B%5B%22thaiFirstName%22%2C%22contains%22%2C%22%E0%B8%AA%E0%B8%A1%22%5D%2C%22or%22%2C%5B%22thaiLastName%22%2C%22contains%22%2C%22%E0%B8%AA%E0%B8%A1%22%5D%5D";

            var result = LearnersController.MapFilterFieldNames(queryString);

            var match = System.Text.RegularExpressions.Regex.Match(result, @"filter=([^&]*)");
            Assert.True(match.Success);
            var decoded = Uri.UnescapeDataString(match.Groups[1].Value);

            Assert.Contains("\"ThaiFirstName\"", decoded);
            Assert.Contains("\"ThaiLastName\"", decoded);
            Assert.DoesNotContain("\"thaiFirstName\"", decoded);
            Assert.Contains("สม", decoded); // search value untouched
        }

        [Fact]
        public void InjectDivisionFilter_WithExistingFormEncodedPlusFilter_InjectsAndPreservesSpaces()
        {
            // query: filter=["section","=","Corporate+Support+Division+(FM)"]
            var queryString = "?filter=%5B%22section%22%2C%22%3D%22%2C%22Corporate+Support+Division+%28FM%29%22%5D";
            
            var result = LearnersController.InjectDivisionFilter(queryString, "NLC");
            
            var match = System.Text.RegularExpressions.Regex.Match(result, @"filter=([^&]*)");
            Assert.True(match.Success);
            var decoded = Uri.UnescapeDataString(match.Groups[1].Value);
            
            // Expected combined filter: [["section","=","Corporate Support Division (FM)"],"and",["Division","=","NLC"]]
            Assert.Contains("\"section\"", decoded);
            Assert.Contains("Corporate Support Division (FM)", decoded);
            Assert.Contains("\"Division\"", decoded);
            Assert.Contains("\"NLC\"", decoded);
            Assert.Contains("\"and\"", decoded);
            Assert.DoesNotContain("+", decoded);
        }
    }
}
