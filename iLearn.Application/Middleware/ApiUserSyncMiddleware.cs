using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace iLearn.Application.Middleware
{
    public class ApiUserSyncMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiUserSyncMiddleware> _logger;
        private readonly IMemoryCache _cache;

        public ApiUserSyncMiddleware(RequestDelegate next, ILogger<ApiUserSyncMiddleware> logger, IMemoryCache cache)
        {
            _next = next;
            _logger = logger;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context, IApiUserService apiUserService)
        {
            // Skip สำหรับ static files เท่านั้น
            if (ShouldSkipMiddleware(context))
            {
                await _next(context);
                return;
            }

            if (context.User.Identity?.IsAuthenticated == true)
            {
                var windowsIdentity = context.User.Identity.Name;
                if (!string.IsNullOrEmpty(windowsIdentity) &&
                    windowsIdentity.StartsWith("NIKONOA\\", StringComparison.OrdinalIgnoreCase))
                {
                    var cacheKey     = $"user_claims_{windowsIdentity}";
                    var forceRefresh = context.Request.Query.ContainsKey("_refresh");

                    // ── ถ้ามี cache และไม่ได้ force refresh → inject Claims จาก cache ทันที ──
                    if (!forceRefresh && _cache.TryGetValue(cacheKey, out ClaimsPrincipal? cached) && cached != null)
                    {
                        context.User = cached;
                        await _next(context);
                        return;
                    }

                    // ── ไม่มี cache หรือ force refresh → sync จาก API ──
                    _logger.LogInformation("Syncing user claims from API for: {WindowsIdentity}", windowsIdentity);
                    try
                    {
                        var userResponse = await apiUserService.GetOrCreateUserAsync(windowsIdentity, forceRefresh);
                        if (userResponse.Success && userResponse.Data != null)
                        {
                            var user = userResponse.Data;

                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, windowsIdentity),
                                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                                new Claim("UserId", user.Id.ToString()),
                                new Claim("FullName", user.FullName ?? ""),
                                new Claim("Email", user.Email ?? "")
                            };

                            // เพิ่ม Role Claims จาก DB
                            foreach (var role in user.Roles ?? [])
                            {
                                claims.Add(new Claim(ClaimTypes.Role, role.Name));
                            }

                            // ── Data Isolation: DivisionId จาก Role แรกที่มีค่า ──
                            var primaryDivisionId = user.Roles?
                                .Where(r => r.DivisionId.HasValue)
                                .Select(r => r.DivisionId!.Value)
                                .FirstOrDefault() ?? 0;

                            if (primaryDivisionId > 0)
                                claims.Add(new Claim("DivisionId", primaryDivisionId.ToString()));

                            var claimsIdentity  = new ClaimsIdentity(claims, context.User.Identity.AuthenticationType);
                            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                            // Inject กลับเข้า HttpContext
                            context.User = claimsPrincipal;

                            // Cache ClaimsPrincipal ไว้ 10 นาที
                            _cache.Set(cacheKey, claimsPrincipal, TimeSpan.FromMinutes(10));

                            _logger.LogInformation(
                                "User {Identity} synced: {RoleCount} role(s) [{Roles}], DivisionId={DivisionId}",
                                windowsIdentity,
                                user.Roles?.Count ?? 0,
                                string.Join(", ", user.Roles?.Select(r => r.Name) ?? []),
                                primaryDivisionId > 0 ? primaryDivisionId.ToString() : "—");
                        }
                        else
                        {
                            _logger.LogWarning("API sync failed for: {WindowsIdentity} — {Message}",
                                windowsIdentity, userResponse.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing Windows user via API: {WindowsIdentity}", windowsIdentity);
                    }
                }
            }

            await _next(context);
        }

        private static bool ShouldSkipMiddleware(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();

            // Skip เฉพาะ static files
            if (path != null && (
                path.StartsWith("/_framework/") ||
                path.StartsWith("/css/") ||
                path.StartsWith("/js/") ||
                path.StartsWith("/lib/") ||
                path.StartsWith("/images/") ||
                path.StartsWith("/favicon.ico") ||
                path.EndsWith(".css") ||
                path.EndsWith(".js") ||
                path.EndsWith(".png") ||
                path.EndsWith(".jpg") ||
                path.EndsWith(".jpeg") ||
                path.EndsWith(".gif") ||
                path.EndsWith(".svg") ||
                path.EndsWith(".ico") ||
                path.EndsWith(".woff") ||
                path.EndsWith(".woff2") ||
                path.EndsWith(".ttf") ||
                path.EndsWith(".eot")))
            {
                return true;
            }

            return false;
        }
    }
}