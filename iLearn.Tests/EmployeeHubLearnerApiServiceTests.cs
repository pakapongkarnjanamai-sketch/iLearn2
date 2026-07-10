using DevExtreme.AspNet.Mvc;
using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace iLearn.Tests
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, Task<HttpResponseMessage>> SendAsyncHandler { get; set; } = null!;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (SendAsyncHandler == null)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
            return await SendAsyncHandler(request);
        }
    }

    public class EmployeeHubLearnerApiServiceTests
    {
        private readonly MockHttpMessageHandler _httpHandler;
        private readonly EmployeeHubClient _client;
        private readonly IMemoryCache _cache;
        private readonly EmployeeHubLearnerApiService _service;

        public EmployeeHubLearnerApiServiceTests()
        {
            _httpHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(_httpHandler)
            {
                BaseAddress = new Uri("http://localhost/Tools/EmployeeHub/Service/")
            };

            _client = new EmployeeHubClient(httpClient);
            _cache = new MemoryCache(new MemoryCacheOptions());
            _service = new EmployeeHubLearnerApiService(_client, _cache, NullLogger<EmployeeHubLearnerApiService>.Instance);
        }

        [Fact]
        public async Task GetActiveEmployeesCachedAsync_CachesResultFor30Min()
        {
            // Arrange
            var callCount = 0;
            _httpHandler.SendAsyncHandler = (req) =>
            {
                callCount++;
                var response = new EmployeeHubPagedResult<EmployeeDto>
                {
                    Items = new List<EmployeeDto>
                    {
                        new EmployeeDto { EmpCode = "E01", Company = "NTC", Division = "CSD" }
                    },
                    Total = 1,
                    Page = 1,
                    PageSize = 200
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            };

            // Act
            var res1 = await _service.GetLearnerAsync();
            var res2 = await _service.GetLearnerAsync();

            // Assert
            Assert.Single(res1.data);
            Assert.Single(res2.data);
            Assert.Equal(1, callCount); // Second call served from cache
        }

        [Fact]
        public async Task GetLearnerByCodeAsync_MapsPropertiesCorrectly()
        {
            // Arrange
            _httpHandler.SendAsyncHandler = (req) =>
            {
                Assert.Contains("api/employees/E01", req.RequestUri!.ToString());
                var emp = new EmployeeDto
                {
                    EmpCode = "E01",
                    FullNameEn = "Hida Motohisa",
                    Section = "Corporate",
                    Division = "CSD",
                    Department = "CSD Dept",
                    Grade = "M1M"
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(emp)
                });
            };

            // Act
            var result = await _service.GetLearnerByCodeAsync("E01");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("E01", result.Code);
            Assert.Equal("Hida Motohisa", result.Name);
            Assert.Equal("Corporate", result.Section);
            Assert.Equal("CSD", result.Division);
            Assert.Equal("CSD Dept", result.Department);
            Assert.Equal("M1M", result.Position); // Position := Grade
        }

        [Fact]
        public async Task GetLearnerByCodeAsync_ReturnsNullOn404()
        {
            // Arrange
            _httpHandler.SendAsyncHandler = (req) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            };

            // Act
            var result = await _service.GetLearnerByCodeAsync("E01");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetLearnersByDivisionsAsync_AppliesSemanticsAndPaging()
        {
            // Arrange
            _httpHandler.SendAsyncHandler = (req) =>
            {
                var response = new EmployeeHubPagedResult<EmployeeDto>
                {
                    Items = new List<EmployeeDto>
                    {
                        new EmployeeDto { EmpCode = "E01", Company = "NLC", Division = "PD", Grade = "G1" },
                        new EmployeeDto { EmpCode = "E02", Company = "NTC", Division = "CSD", Grade = "G2" },
                        new EmployeeDto { EmpCode = "E03", Company = "VDS", Division = "PD3", Grade = "G3" },
                        new EmployeeDto { EmpCode = "E04", Company = "NTC", Division = "PD3", Grade = "G4" }
                    },
                    Total = 4,
                    Page = 1,
                    PageSize = 200
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            };

            // Act: query NLC -> matches E01 (Company == NLC)
            var nlcResult = await _service.GetLearnersByDivisionsAsync(new[] { "NLC" }, 0, 10);
            // Act: query PD3 -> matches E03, E04 (Division == PD3)
            var pd3Result = await _service.GetLearnersByDivisionsAsync(new[] { "PD3" }, 0, 10);

            // Assert
            Assert.Single(nlcResult.data);
            Assert.Equal("E01", nlcResult.data[0].EId);
            Assert.Equal("G1", nlcResult.data[0].Position);

            Assert.Equal(2, pd3Result.data.Count);
            Assert.Equal("E03", pd3Result.data[0].EId);
            Assert.Equal("E04", pd3Result.data[1].EId);
        }

        [Fact]
        public async Task GetEmployeesByNidsAsync_ChunksBy200()
        {
            // Arrange
            var batchCount = 0;
            _httpHandler.SendAsyncHandler = (req) =>
            {
                batchCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new FindByNidsResultDto { Count = 0, Items = new List<EmployeeDto>() })
                });
            };

            var nids = Enumerable.Range(1, 250).Select(i => $"NID{i}").ToList();

            // Act
            await _service.GetEmployeesByNidsAsync(nids);

            // Assert
            Assert.Equal(2, batchCount); // 250 NIDs chunked into 200 and 50 -> 2 API calls
        }

        [Fact]
        public async Task GetLearnersDxGridAsync_ParsesDevExtremeOptions()
        {
            // Arrange
            _httpHandler.SendAsyncHandler = (req) =>
            {
                var response = new EmployeeHubPagedResult<EmployeeDto>
                {
                    Items = new List<EmployeeDto>
                    {
                        new EmployeeDto { EmpCode = "001", Company = "NTC", Division = "CSD", FirstNameEn = "John", LastNameEn = "Doe" },
                        new EmployeeDto { EmpCode = "002", Company = "NTC", Division = "CSD", FirstNameEn = "Jane", LastNameEn = "Doe" },
                        new EmployeeDto { EmpCode = "003", Company = "NTC", Division = "PD", FirstNameEn = "Jack", LastNameEn = "Smith" }
                    },
                    Total = 3,
                    Page = 1,
                    PageSize = 200
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            };

            // Act: skip=1, take=1, filter on Division == CSD (mapped items contain John & Jane, John is skipped, Jane is returned)
            var queryString = "?skip=1&take=1&requireTotalCount=true&filter=%5B%22division%22%2C%22%3D%22%2C%22CSD%22%5D";
            var resultJson = await _service.GetLearnersDxGridAsync(queryString);

            // Deserialize to check shape
            var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;
            var data = root.GetProperty("data");
            var totalCount = root.GetProperty("totalCount").GetInt32();

            // Assert
            Assert.Equal(2, totalCount); // John and Jane match division=CSD
            Assert.Equal(1, data.GetArrayLength()); // take = 1
            Assert.Equal("002", data[0].GetProperty("eId").GetString()); //Jane is second
        }

        [Fact]
        public async Task GetDivisionsAsync_AppliesDistinctCompanySemantics()
        {
            // Arrange
            _httpHandler.SendAsyncHandler = (req) =>
            {
                var response = new EmployeeHubPagedResult<EmployeeDto>
                {
                    Items = new List<EmployeeDto>
                    {
                        new EmployeeDto { EmpCode = "1", Company = "NLC", Division = "PD_LA" }, // NLC divisions are ignored in lookups
                        new EmployeeDto { EmpCode = "2", Company = "NTC", Division = "CSD" },
                        new EmployeeDto { EmpCode = "3", Company = "VDS", Division = "PD3" },
                        new EmployeeDto { EmpCode = "4", Company = "NTC", Division = "CSD" } // duplicate CSD
                    },
                    Total = 4,
                    Page = 1,
                    PageSize = 200
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            };

            // Act
            var divisionsObj = await _service.GetDivisionsAsync("");
            
            // Cast to bare collection result
            var divisionsList = ((IEnumerable<LookupNameDto>)divisionsObj).ToList();

            // Assert: expect NLC + CSD + PD3 (sorted: CSD, PD3 -> NLC is first, then rest sorted A-Z)
            Assert.Equal(3, divisionsList.Count);
            Assert.Equal("NLC", divisionsList[0].Name);
            Assert.Equal("CSD", divisionsList[1].Name);
            Assert.Equal("PD3", divisionsList[2].Name);
        }

        [Fact]
        public async Task GetPositionsAsync_ReturnsDistinctGrades()
        {
            // Arrange
            _httpHandler.SendAsyncHandler = (req) =>
            {
                var response = new EmployeeHubPagedResult<EmployeeDto>
                {
                    Items = new List<EmployeeDto>
                    {
                        new EmployeeDto { EmpCode = "1", Grade = "M1M" },
                        new EmployeeDto { EmpCode = "2", Grade = "G1" },
                        new EmployeeDto { EmpCode = "3", Grade = "M1M" } // duplicate M1M
                    },
                    Total = 3,
                    Page = 1,
                    PageSize = 200
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            };

            // Act
            var positionsObj = await _service.GetPositionsAsync("");
            var positionsList = ((IEnumerable<LookupNameDto>)positionsObj).ToList();

            // Assert: expect G1 and M1M (distinct and sorted)
            Assert.Equal(2, positionsList.Count);
            Assert.Equal("G1", positionsList[0].Name);
            Assert.Equal("M1M", positionsList[1].Name);
        }

        [Fact]
        public async Task GetDepartmentsAsync_FiltersByDivisionBeforeProjecting()
        {
            // Arrange
            _httpHandler.SendAsyncHandler = (req) =>
            {
                var response = new EmployeeHubPagedResult<EmployeeDto>
                {
                    Items = new List<EmployeeDto>
                    {
                        new EmployeeDto { EmpCode = "1", Company = "NTC", Division = "CSD", Department = "CSD Dept" },
                        new EmployeeDto { EmpCode = "2", Company = "NTC", Division = "CSD", Department = "CSD QA Dept" },
                        new EmployeeDto { EmpCode = "3", Company = "NTC", Division = "PD", Department = "PD Dept" }
                    },
                    Total = 3,
                    Page = 1,
                    PageSize = 200
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            };

            // Act: filter for Division = CSD
            var result = await _service.GetDepartmentsAsync("?filter=%5B%22Division%22%2C%22%3D%22%2C%22CSD%22%5D");
            var list = ((IEnumerable<LookupNameDto>)result).ToList();

            // Assert: expect only CSD Dept and CSD QA Dept (sorted)
            Assert.Equal(2, list.Count);
            Assert.Equal("CSD Dept", list[0].Name);
            Assert.Equal("CSD QA Dept", list[1].Name);
        }

        [Fact]
        public async Task GetSectionsAsync_FiltersByDivisionAndDepartmentBeforeProjecting()
        {
            // Arrange
            _httpHandler.SendAsyncHandler = (req) =>
            {
                var response = new EmployeeHubPagedResult<EmployeeDto>
                {
                    Items = new List<EmployeeDto>
                    {
                        new EmployeeDto { EmpCode = "1", Company = "NTC", Division = "CSD", Department = "CSD Dept", Section = "CSD Sec A" },
                        new EmployeeDto { EmpCode = "2", Company = "NTC", Division = "CSD", Department = "CSD QA Dept", Section = "CSD Sec B" }, // wrong dept
                        new EmployeeDto { EmpCode = "3", Company = "NTC", Division = "PD", Department = "PD Dept", Section = "PD Sec A" } // wrong div
                    },
                    Total = 3,
                    Page = 1,
                    PageSize = 200
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            };

            // Act: filter for Division = CSD and Department = CSD Dept
            var result = await _service.GetSectionsAsync("?filter=%5B%5B%22Division%22%2C%22%3D%22%2C%22CSD%22%5D%2C%22and%22%2C%5B%22Department%22%2C%22%3D%22%2C%22CSD+Dept%22%5D%5D");
            var list = ((IEnumerable<LookupNameDto>)result).ToList();

            // Assert: expect only CSD Sec A
            Assert.Single(list);
            Assert.Equal("CSD Sec A", list[0].Name);
        }

        [Fact]
        public async Task GetLearnersDxGridAsync_WithNlcDivisionFilter_ReturnsOnlyNlcEmployeesAndDivisionAsNlc()
        {
            // Arrange
            _httpHandler.SendAsyncHandler = (req) =>
            {
                var response = new EmployeeHubPagedResult<EmployeeDto>
                {
                    Items = new List<EmployeeDto>
                    {
                        new EmployeeDto { EmpCode = "001", Company = "NLC", Division = "PD", FirstNameEn = "John", LastNameEn = "Doe" },
                        new EmployeeDto { EmpCode = "002", Company = "NTC", Division = "CSD", FirstNameEn = "Jane", LastNameEn = "Doe" },
                        new EmployeeDto { EmpCode = "003", Company = "NLC", Division = "AD", FirstNameEn = "Jack", LastNameEn = "Smith" }
                    },
                    Total = 3,
                    Page = 1,
                    PageSize = 200
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            };

            // Act: filter on Division == NLC
            var queryString = "?requireTotalCount=true&filter=%5B%22Division%22%2C%22%3D%22%2C%22NLC%22%5D";
            var resultJson = await _service.GetLearnersDxGridAsync(queryString);

            // Deserialize to check shape
            var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;
            var data = root.GetProperty("data");
            var totalCount = root.GetProperty("totalCount").GetInt32();

            // Assert
            Assert.Equal(2, totalCount); // 001 and 003 are NLC because their Company is NLC and normalized
            Assert.Equal(2, data.GetArrayLength());
            Assert.Equal("001", data[0].GetProperty("eId").GetString());
            Assert.Equal("NLC", data[0].GetProperty("division").GetString());
            Assert.Equal("003", data[1].GetProperty("eId").GetString());
            Assert.Equal("NLC", data[1].GetProperty("division").GetString());
        }

        [Fact]
        public async Task GetLearnerByCodeAsync_ForNlcEmployee_NormalizesDivisionToNlc()
        {
            // Arrange
            _httpHandler.SendAsyncHandler = (req) =>
            {
                var emp = new EmployeeDto
                {
                    EmpCode = "E01",
                    FullNameEn = "Hida Motohisa",
                    Company = "NLC",
                    Division = "PD",
                    Section = "Corporate",
                    Department = "CSD Dept",
                    Grade = "M1M"
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(emp)
                });
            };

            // Act
            var result = await _service.GetLearnerByCodeAsync("E01");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("E01", result.Code);
            Assert.Equal("NLC", result.Division); // normalized from PD to NLC
        }

        [Fact]
        public async Task GetDepartmentsAsync_WithNlcDivisionFilter_ReturnsNlcDepartments()
        {
            // Arrange
            _httpHandler.SendAsyncHandler = (req) =>
            {
                var response = new EmployeeHubPagedResult<EmployeeDto>
                {
                    Items = new List<EmployeeDto>
                    {
                        new EmployeeDto { EmpCode = "1", Company = "NLC", Division = "PD", Department = "NLC Dept A" },
                        new EmployeeDto { EmpCode = "2", Company = "NTC", Division = "CSD", Department = "CSD QA Dept" },
                        new EmployeeDto { EmpCode = "3", Company = "NLC", Division = "AD", Department = "NLC Dept B" }
                    },
                    Total = 3,
                    Page = 1,
                    PageSize = 200
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            };

            // Act: filter for Division = NLC
            var result = await _service.GetDepartmentsAsync("?filter=%5B%22Division%22%2C%22%3D%22%2C%22NLC%22%5D");
            var list = ((IEnumerable<LookupNameDto>)result).ToList();

            // Assert: expect NLC Dept A and NLC Dept B (sorted)
            Assert.Equal(2, list.Count);
            Assert.Equal("NLC Dept A", list[0].Name);
            Assert.Equal("NLC Dept B", list[1].Name);
        }

        [Fact]
        public async Task GetEmployeesByNidsAsync_ForNlcEmployee_NormalizesDivisionToNlc()
        {
            // Arrange
            _httpHandler.SendAsyncHandler = (req) =>
            {
                var response = new FindByNidsResultDto
                {
                    Count = 1,
                    Items = new List<EmployeeDto>
                    {
                        new EmployeeDto
                        {
                            EmpCode = "E01",
                            Nid = "NID01",
                            Company = "NLC",
                            Division = "PD",
                            FirstNameEn = "John",
                            LastNameEn = "Doe"
                        }
                    }
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            };

            // Act
            var result = await _service.GetEmployeesByNidsAsync(new[] { "NID01" });

            // Assert
            Assert.True(result.ContainsKey("NID01"));
            Assert.Equal("NLC", result["NID01"].Division); // normalized from PD to NLC
        }
    }
}
