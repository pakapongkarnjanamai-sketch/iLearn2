using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace iLearn.API.Controllers.Base
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class UsersCRUDController : GenericController<User>
    {
        private readonly IGenericRepository<UserRole> _userRoleRepo;
        private readonly ILearnerApiService _learnerApiService;

        public UsersCRUDController(
            IGenericRepository<User> repository,
            ICurrentUserService currentUser,
            IGenericRepository<UserRole> userRoleRepo,
            ILearnerApiService learnerApiService) : base(repository, currentUser)
        {
            _userRoleRepo = userRoleRepo;
            _learnerApiService = learnerApiService;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {

            IQueryable<User> query = _repository.GetQuery()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

            // -- Data Isolation: Admin ????????? User ?? Division ?????? --
            if (_currentUser.DivisionId.HasValue)
            {
                var myDivId = _currentUser.DivisionId.Value;
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.DivisionId == myDivId));
            }

            var projected = query.Select(u => new
            {
                u.Id,
                u.Nid,
                u.LastLogin,
                u.CreatedAt,
                u.IsActive,
                UserRoles = u.UserRoles.Select(ur => new
                {
                    ur.UserId,
                    ur.RoleId,
                    Role = ur.Role == null ? null : new
                    {
                        ur.Role.Id,
                        ur.Role.Name,
                        ur.Role.RoleType,
                        ur.Role.DivisionId
                    },
                    ur.Id,
                    ur.IsActive,
                    ur.CreatedAt,
                    ur.UpdatedAt,
                    ur.CreatedBy,
                    ur.UpdatedBy,
                    ur.IsDeleted,
                    ur.DeletedAt,
                    ur.DeletedBy
                }).ToList()
            });

            // Use EF Core async retrieval if supported (e.g. database-backed query), otherwise fallback to sync (e.g. in-memory test queryable)
            var usersFromDb = (projected.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
                ? await projected.ToListAsync()
                : projected.ToList();

            // 2. Lookup employee enrichment data in a single batch
            var employeeLookup = await _learnerApiService.GetEmployeesByNidsAsync(
                usersFromDb.Select(u => u.Nid ?? string.Empty));

            // 3. Map to the enriched projection (in-memory)
            // Note: This collection is small as it only contains active admin users.
            // If the admin user base grows significantly in the future, we may need to implement a database-level join.
            var enrichedList = usersFromDb.Select(u =>
            {
                employeeLookup.TryGetValue(u.Nid ?? string.Empty, out var employee);

                return new
                {
                    u.Id,
                    u.Nid,
                    u.LastLogin,
                    u.CreatedAt,
                    u.IsActive,
                    u.UserRoles,
                    EmployeeId = employee?.EId ?? string.Empty,
                    FullName = employee?.FullName ?? string.Empty,
                    Email = employee?.Email ?? string.Empty,
                    Division = employee?.Division ?? string.Empty,
                    Department = employee?.Department ?? string.Empty,
                    Section = employee?.Section ?? string.Empty,
                    Position = employee?.Position ?? string.Empty
                };
            }).ToList();

            // 4. Perform filter/sort/paging on the enriched in-memory list
            var loadResult = DataSourceLoader.Load(enrichedList.AsQueryable(), loadOptions);

            return Ok(loadResult);
        }

        [HttpPut("Put")]
        public override async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var user = await _repository.GetByIdAsync(key);
            if (user == null) return NotFound();

            JsonConvert.PopulateObject(values, user);

            var valuesDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);
            var roleKey    = valuesDict.Keys.FirstOrDefault(k => k.Equals("roleIds", StringComparison.OrdinalIgnoreCase));

            if (roleKey != null)
            {
                var newRoleIds        = JsonConvert.DeserializeObject<List<int>>(valuesDict[roleKey].ToString()) ?? new List<int>();
                var existingUserRoles = (await _userRoleRepo.GetAsync(ur => ur.UserId == key)).ToList();

                foreach (var ur in existingUserRoles)
                {
                    if (!newRoleIds.Contains(ur.RoleId))
                        await _userRoleRepo.DeleteAsync(ur);
                }

                foreach (var roleId in newRoleIds)
                {
                    if (!existingUserRoles.Any(ur => ur.RoleId == roleId))
                        await _userRoleRepo.AddAsync(new UserRole { UserId = key, RoleId = roleId });
                }
            }

            await _repository.UpdateAsync(user);
            return Ok(user);
        }
    }
}
