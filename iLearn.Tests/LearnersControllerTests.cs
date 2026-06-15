using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using iLearn.API.Controllers;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Interfaces.Repositories;
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

            public Task<string> GetLearnersDxGridAsync(string queryString)
            {
                return GetLearnersDxGridAsyncHandler != null 
                    ? GetLearnersDxGridAsyncHandler(queryString) 
                    : Task.FromResult("{\"data\": [], \"totalCount\": 0}");
            }

            public Task<ExternalLearnerDto> GetLearnerByCodeAsync(string Code) => throw new NotImplementedException();
            public Task<AllLearnersApiResponse> GetLearnerAsync() => throw new NotImplementedException();
            public Task<DivisionApiResponse> GetLearnersByDivisionsAsync(string[] divisions, int skip = 0, int take = 20) => throw new NotImplementedException();
            public Task<object> GetSectionsAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetDivisionsAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetDepartmentsAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetPositionsAsync(string queryString) => throw new NotImplementedException();
            public Task<Dictionary<string, ExternalLearnerDto>> GetLearnersByCodesAsync(IEnumerable<string> codes) => throw new NotImplementedException();
            public Task<Dictionary<string, EmployeeCsvDto>> GetEmployeesByNidsAsync(IEnumerable<string> nids) => throw new NotImplementedException();
        }
    }
}
