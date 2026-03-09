using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;

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
    }
}