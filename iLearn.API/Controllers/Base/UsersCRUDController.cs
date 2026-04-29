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

            var loadResult = DataSourceLoader.Load(projected, loadOptions);

            if (loadResult.data is not IEnumerable<object> items)
                return Ok(loadResult);

            var rows = items.Cast<dynamic>().ToList();
            var employeeLookup = await _learnerApiService.GetEmployeesByNidsAsync(
                rows.Select(r => (string?)r.Nid ?? string.Empty));

            var enriched = rows.Select(r =>
            {
                employeeLookup.TryGetValue((string?)r.Nid ?? string.Empty, out var employee);

                return new
                {
                    r.Id,
                    r.Nid,
                    r.LastLogin,
                    r.CreatedAt,
                    r.IsActive,
                    r.UserRoles,
                    EmployeeId = employee?.EId ?? string.Empty,
                    FullName = employee?.FullName ?? string.Empty,
                    Email = employee?.Email ?? string.Empty,
                    Division = employee?.Division ?? string.Empty,
                    Department = employee?.Department ?? string.Empty,
                    Section = employee?.Section ?? string.Empty,
                    Position = employee?.Position ?? string.Empty
                };
            }).ToList();

            return Ok(new
            {
                loadResult.totalCount,
                loadResult.groupCount,
                loadResult.summary,
                data = enriched
            });
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
