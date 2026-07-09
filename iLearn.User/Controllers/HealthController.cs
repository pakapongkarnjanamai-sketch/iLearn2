using System.Diagnostics;
using iLearn.User.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.User.Controllers
{
    /// <summary>
    /// Smoke test endpoints ของเว็บผู้เรียน — ตรวจว่า course static files (/Courses/{id}/res/...)
    /// พร้อม serve และ API ปลายทางตอบ เปิด anonymous เพื่อให้ monitoring เรียกได้โดยไม่ต้อง login
    /// </summary>
    [AllowAnonymous]
    [Route("health")]
    public class HealthController : Controller
    {
        private const int ApiTimeoutSeconds = 10;

        private readonly IHttpClientFactory _httpClientFactory;

        public HealthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>Liveness — โปรเซสยังรับ request ได้</summary>
        [HttpGet("live")]
        public IActionResult Live() => Json(new
        {
            status = "pass",
            service = "iLearn.User",
            timestamp = DateTime.UtcNow,
        });

        /// <summary>
        /// Smoke test — คืน 200 เมื่อผ่านทุกข้อ, 503 เมื่อมีข้อใดล้ม
        /// ระบุ ?courseId=&lt;guid&gt; เพื่อตรวจว่าไฟล์ res/index.html ของ course นั้นมีอยู่จริง
        /// (เคสเดียวกับ URL ผู้เรียน /Courses/{id}/res/index.html)
        /// </summary>
        [HttpGet("")]
        [HttpGet("smoke")]
        public async Task<IActionResult> Smoke([FromQuery] string? courseId, CancellationToken cancellationToken)
        {
            var checks = new List<object>();
            var healthy = true;

            healthy &= await RunCheckAsync(checks, "courseContentFolder", () =>
            {
                var path = CourseContentStatus.PhysicalPath;
                var existsNow = !string.IsNullOrEmpty(path) && Directory.Exists(path);

                if (!CourseContentStatus.MountedAtStartup)
                {
                    return Task.FromResult(existsNow
                        ? (false, $"Folder exists now but static middleware was not mounted at startup — restart the app: {path}")
                        : (false, $"Course content folder not reachable (middleware not mounted): {path}"));
                }

                return Task.FromResult(existsNow
                    ? (true, $"Serving {CourseContentStatus.RequestPath} from {path}")
                    : (false, $"Course content folder no longer reachable: {path}"));
            });

            if (!string.IsNullOrWhiteSpace(courseId))
            {
                healthy &= await RunCheckAsync(checks, "courseIndexFile", () =>
                {
                    if (!Guid.TryParse(courseId, out var parsedCourseId))
                        return Task.FromResult((false, "courseId must be a GUID"));

                    var indexPath = Path.Combine(
                        CourseContentStatus.PhysicalPath, parsedCourseId.ToString(), "res", "index.html");
                    return Task.FromResult(System.IO.File.Exists(indexPath)
                        ? (true, $"Found {parsedCourseId}/res/index.html")
                        : (false, $"Missing course entry file: {indexPath}"));
                });
            }

            healthy &= await RunCheckAsync(checks, "api", async () =>
            {
                var client = _httpClientFactory.CreateClient("iLearnAPI");
                client.Timeout = TimeSpan.FromSeconds(ApiTimeoutSeconds);

                using var response = await client.GetAsync("health/live", cancellationToken);
                return response.IsSuccessStatusCode
                    ? (true, $"API reachable ({(int)response.StatusCode})")
                    : (false, $"API returned {(int)response.StatusCode} from {client.BaseAddress}health/live");
            });

            var payload = new
            {
                status = healthy ? "pass" : "fail",
                service = "iLearn.User",
                timestamp = DateTime.UtcNow,
                checks,
            };

            return healthy
                ? Json(payload)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
        }

        private static async Task<bool> RunCheckAsync(
            List<object> checks,
            string name,
            Func<Task<(bool Passed, string Detail)>> check)
        {
            var stopwatch = Stopwatch.StartNew();
            bool passed;
            string detail;

            try
            {
                (passed, detail) = await check();
            }
            catch (Exception ex)
            {
                passed = false;
                detail = ex.GetBaseException().Message;
            }

            stopwatch.Stop();
            checks.Add(new
            {
                name,
                status = passed ? "pass" : "fail",
                detail,
                durationMs = stopwatch.ElapsedMilliseconds,
            });

            return passed;
        }
    }
}
