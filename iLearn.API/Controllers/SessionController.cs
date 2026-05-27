using System.Security.Claims;
using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/admin/session")]
    [ApiController]
    [Authorize(Policy = "DomainUser")]
    public class SessionController : ControllerBase
    {
        private readonly ICurrentUserService _currentUser;

        public SessionController(ICurrentUserService currentUser)
        {
            _currentUser = currentUser;
        }

        [HttpGet("me")]
        public IActionResult Me()
        {
            var principal = User;
            var isAuthenticated = principal?.Identity?.IsAuthenticated == true;

            var roles = principal?
                .FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];

            var displayName = principal?.FindFirst("DisplayName")?.Value
                              ?? principal?.Identity?.Name
                              ?? _currentUser.UserId;

            return Ok(new
            {
                isAuthenticated,
                nid = _currentUser.UserId,
                displayName,
                divisionId = _currentUser.DivisionId,
                divisionName = _currentUser.DivisionName,
                isSuperAdmin = _currentUser.IsSuperAdmin,
                roles,
            });
        }
    }
}
