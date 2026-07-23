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
