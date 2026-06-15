using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DevExtreme.AspNet.Mvc;
using iLearn.API.Controllers.Base;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace iLearn.Tests
{
    public sealed class UsersCRUDControllerTests
    {
        [Fact]
        public async Task Get_ReturnsEnrichedAndPagedAdminUsers()
        {
            // Arrange
            var role1 = new Role { Id = 101, Name = "SuperAdmin", DivisionId = null };
            var users = new List<User>
            {
                new()
                {
                    Id = 1,
                    Nid = "NID001",
                    IsActive = true,
                    UserRoles = new List<UserRole>
                    {
                        new() { Id = 1, UserId = 1, RoleId = 101, Role = role1, IsActive = true }
                    }
                },
                new()
                {
                    Id = 2,
                    Nid = "NID002",
                    IsActive = true,
                    UserRoles = new List<UserRole>()
                }
            };

            var fakeEmployees = new Dictionary<string, EmployeeCsvDto>(StringComparer.OrdinalIgnoreCase)
            {
                { "NID001", new EmployeeCsvDto { EId = "EMP001", EnglishFirstName = "John", EnglishLastName = "Doe", NID = "NID001", Division = "Engineering", Department = "IT", Position = "Manager" } },
                { "NID002", new EmployeeCsvDto { EId = "EMP002", EnglishFirstName = "Jane", EnglishLastName = "Smith", NID = "NID002", Division = "HR", Department = "Recruiting", Position = "Specialist" } }
            };

            var fakeLearnerService = new FakeLearnerApiService { Employees = fakeEmployees };
            var fakeCurrentUser = new FakeCurrentUserService();

            var controller = new UsersCRUDController(
                new InMemoryGenericRepository<User>(users),
                fakeCurrentUser,
                null!,
                fakeLearnerService);

            // Case 1: No filters, should get both users, enriched
            var loadOptions1 = new DataSourceLoadOptions { RequireTotalCount = true };
            var result1 = await controller.Get(loadOptions1);
            var okResult1 = Assert.IsType<OkObjectResult>(result1);
            var value1 = okResult1.Value!;
            var dataProp1 = value1.GetType().GetProperty("data")!;
            var totalCountProp1 = value1.GetType().GetProperty("totalCount")!;

            var dataList1 = ((System.Collections.IEnumerable)dataProp1.GetValue(value1)!).Cast<object>().ToList();
            Assert.Equal(2, dataList1.Count);
            Assert.Equal(2, (int)totalCountProp1.GetValue(value1)!);

            var firstUser = dataList1.First(u => (int)u.GetType().GetProperty("Id")!.GetValue(u)! == 1);
            Assert.Equal("NID001", (string)firstUser.GetType().GetProperty("Nid")!.GetValue(firstUser)!);
            Assert.Equal("John Doe", (string)firstUser.GetType().GetProperty("FullName")!.GetValue(firstUser)!);
            Assert.Equal("Engineering", (string)firstUser.GetType().GetProperty("Division")!.GetValue(firstUser)!);

            // Case 2: Filter by Division in memory (simulating the client-side search parameter)
            // DevExtreme filter expression for "Division contains Engineering"
            var loadOptions2 = new DataSourceLoadOptions
            {
                Filter = new List<object> { "Division", "contains", "Engineering" }
            };
            var result2 = await controller.Get(loadOptions2);
            var okResult2 = Assert.IsType<OkObjectResult>(result2);
            var value2 = okResult2.Value!;
            var dataProp2 = value2.GetType().GetProperty("data")!;

            var dataList2 = ((System.Collections.IEnumerable)dataProp2.GetValue(value2)!).Cast<object>().ToList();
            var matchedUser = Assert.Single(dataList2);
            Assert.Equal("NID001", (string)matchedUser.GetType().GetProperty("Nid")!.GetValue(matchedUser)!);
            Assert.Equal("Engineering", (string)matchedUser.GetType().GetProperty("Division")!.GetValue(matchedUser)!);
        }

        [Fact]
        public async Task Get_AppliesDivisionIsolationCorrectly()
        {
            // Arrange
            var roleHR = new Role { Id = 201, Name = "HRAdmin", DivisionId = 5 };
            var roleIT = new Role { Id = 202, Name = "ITAdmin", DivisionId = 10 };
            
            var users = new List<User>
            {
                new()
                {
                    Id = 1,
                    Nid = "NID001",
                    IsActive = true,
                    UserRoles = new List<UserRole>
                    {
                        new() { Id = 1, UserId = 1, RoleId = 201, Role = roleHR, IsActive = true }
                    }
                },
                new()
                {
                    Id = 2,
                    Nid = "NID002",
                    IsActive = true,
                    UserRoles = new List<UserRole>
                    {
                        new() { Id = 2, UserId = 2, RoleId = 202, Role = roleIT, IsActive = true }
                    }
                }
            };

            var fakeEmployees = new Dictionary<string, EmployeeCsvDto>(StringComparer.OrdinalIgnoreCase)
            {
                { "NID001", new EmployeeCsvDto { EId = "EMP001", EnglishFirstName = "John", EnglishLastName = "HR", NID = "NID001", Division = "HR" } },
                { "NID002", new EmployeeCsvDto { EId = "EMP002", EnglishFirstName = "Jane", EnglishLastName = "IT", NID = "NID002", Division = "IT" } }
            };

            var fakeLearnerService = new FakeLearnerApiService { Employees = fakeEmployees };
            var fakeCurrentUser = new FakeCurrentUserService
            {
                DivisionId = 5,
                DivisionName = "HR"
            };

            var controller = new UsersCRUDController(
                new InMemoryGenericRepository<User>(users),
                fakeCurrentUser,
                null!,
                fakeLearnerService);

            var loadOptions = new DataSourceLoadOptions();
            var result = await controller.Get(loadOptions);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value!;
            var dataProp = value.GetType().GetProperty("data")!;

            var dataList = ((System.Collections.IEnumerable)dataProp.GetValue(value)!).Cast<object>().ToList();
            
            // Only user 1 has Role with DivisionId = 5, so only user 1 should be visible
            var visibleUser = Assert.Single(dataList);
            Assert.Equal(1, (int)visibleUser.GetType().GetProperty("Id")!.GetValue(visibleUser)!);
            Assert.Equal("NID001", (string)visibleUser.GetType().GetProperty("Nid")!.GetValue(visibleUser)!);
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public string UserId => "1";
            public string FullName => "Test Admin";
            public bool IsAuthenticated => true;
            public int? DivisionId { get; set; }
            public string? DivisionName { get; set; }
            public bool IsSuperAdmin => true;
        }

        private sealed class FakeLearnerApiService : ILearnerApiService
        {
            public Dictionary<string, EmployeeCsvDto> Employees { get; set; } = new(StringComparer.OrdinalIgnoreCase);

            public Task<Dictionary<string, EmployeeCsvDto>> GetEmployeesByNidsAsync(IEnumerable<string> nids)
            {
                var result = nids
                    .Where(n => Employees.ContainsKey(n))
                    .ToDictionary(n => n, n => Employees[n], StringComparer.OrdinalIgnoreCase);
                return Task.FromResult(result);
            }

            public Task<ExternalLearnerDto> GetLearnerByCodeAsync(string Code) => throw new NotImplementedException();
            public Task<AllLearnersApiResponse> GetLearnerAsync() => throw new NotImplementedException();
            public Task<DivisionApiResponse> GetLearnersByDivisionsAsync(string[] divisions, int skip = 0, int take = 20) => throw new NotImplementedException();
            public Task<string> GetLearnersDxGridAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetSectionsAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetDivisionsAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetDepartmentsAsync(string queryString) => throw new NotImplementedException();
            public Task<object> GetPositionsAsync(string queryString) => throw new NotImplementedException();
            public Task<Dictionary<string, ExternalLearnerDto>> GetLearnersByCodesAsync(IEnumerable<string> codes) => throw new NotImplementedException();
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
