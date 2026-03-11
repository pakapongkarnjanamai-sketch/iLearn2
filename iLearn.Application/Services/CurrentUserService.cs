using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace iLearn.Application.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string UserId
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;

                // กรณีไม่มี User Login หรือเป็น System ให้ return "SYSTEM" หรือค่าว่างตาม Business Rule
                if (user?.Identity?.IsAuthenticated != true)
                    return "SYSTEM";

                var fullName = user.Identity.Name; // ex: "NIKONOA\N4734"
                if (string.IsNullOrEmpty(fullName))
                    return "SYSTEM";

                // Logic ตัด Domain: ถ้ามี Backslash ให้เอาข้างหลัง, ถ้าไม่มีให้เอาทั้งหมด
                var parts = fullName.Split('\\');
                return parts.Length > 1 ? parts[1] : parts[0];
            }
        }

        public string FullName => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "SYSTEM";

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public int? DivisionId
        {
            get
            {
                // SuperAdmin เห็นทุก Division → return null เพื่อข้ามการกรอง
                if (IsSuperAdmin)
                    return null;

                var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirst("DivisionId")?.Value;
                if (int.TryParse(claimValue, out var id))
                    return id;
                return null;
            }
        }

        public string? DivisionName =>
            _httpContextAccessor.HttpContext?.User?.FindFirst("Division")?.Value;

        public bool IsSuperAdmin =>
            // เพิ่มชื่อ Role ที่ถือว่าเป็นสิทธิ์สูงสุดของระบบคุณเข้าไป (ให้ตรงกับชื่อ Role ในฐานข้อมูล)
            (_httpContextAccessor.HttpContext?.User?.IsInRole("SuperAdmin") ?? false) ||
            (_httpContextAccessor.HttpContext?.User?.IsInRole("Develop") ?? false) ||
            (_httpContextAccessor.HttpContext?.User?.IsInRole("Developer") ?? false) ||
            (_httpContextAccessor.HttpContext?.User?.IsInRole("Administrator") ?? false);
    }
}