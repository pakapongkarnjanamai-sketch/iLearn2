using System.Diagnostics;
using System.Net.Http;
using iLearn.Application.Common;
using iLearn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace iLearn.API.Controllers
{
    /// <summary>
    /// Smoke test endpoints สำหรับตรวจสถานะความพร้อมของ API และ dependency ปลายทาง
    /// (database, SCORM course file share) — เปิด anonymous เพื่อให้ monitoring/deploy script
    /// เรียกได้โดยไม่ต้องผ่าน Windows auth และไม่เปิดเผยข้อมูล config ที่อ่อนไหว
    /// </summary>
    [AllowAnonymous]
    [Route("api/health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly FileSettings _fileSettings;
        private readonly EmployeeServiceSettings _employeeSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public HealthController(
            AppDbContext db,
            IOptions<FileSettings> fileSettings,
            IOptions<EmployeeServiceSettings> employeeSettings,
            IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _fileSettings = fileSettings.Value;
            _employeeSettings = employeeSettings.Value;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>Liveness — โปรเซสยังรับ request ได้ (ใช้กับ -HealthCheckUrl ของ deploy script)</summary>
        [HttpGet("live")]
        public IActionResult Live() => Ok(new
        {
            status = "pass",
            service = "iLearn.API",
            timestamp = DateTime.UtcNow,
        });

        /// <summary>Smoke test — ตรวจ dependency ที่ API ต้องใช้จริง คืน 200 เมื่อผ่านทุกข้อ, 503 เมื่อมีข้อใดล้ม</summary>
        [HttpGet]
        [HttpGet("smoke")]
        public async Task<IActionResult> Smoke(CancellationToken cancellationToken)
        {
            var checks = new List<object>();
            var healthy = true;

            healthy &= await RunCheckAsync(checks, "database", async () =>
            {
                var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
                return canConnect
                    ? (true, "Connected")
                    : (false, "Cannot connect to database");
            });

            healthy &= await RunCheckAsync(checks, "courseFileShare", () =>
            {
                if (string.IsNullOrWhiteSpace(_fileSettings.HostUnc))
                    return Task.FromResult((true, "HostUnc not configured — local course storage assumed"));

                var path = _fileSettings.FileUnc;
                return Task.FromResult(Directory.Exists(path)
                    ? (true, "Course file share reachable")
                    : (false, $"Course file share not reachable: {path}"));
            });

            healthy &= await RunCheckAsync(checks, "employeeDirectory", async () =>
            {
                var provider = _employeeSettings.Provider;
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                if (string.Equals(provider, "EmployeeHub", StringComparison.OrdinalIgnoreCase))
                {
                    var url = _employeeSettings.EmployeeHubBaseUrl;
                    if (string.IsNullOrWhiteSpace(url))
                        return (false, "EmployeeHub URL is not configured");

                    var targetUrl = url.EndsWith("/") ? $"{url}health" : $"{url}/health";
                    var response = await client.GetAsync(targetUrl, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync(cancellationToken);
                        return (true, $"EmployeeHub (Healthy) - {content.Trim()}");
                    }
                    else
                    {
                        return (false, $"EmployeeHub returned status {(int)response.StatusCode} for health check");
                    }
                }
                else
                {
                    var url = _employeeSettings.BaseLearnerLookupUrl;
                    if (string.IsNullOrWhiteSpace(url))
                        return (false, "Legacy base learner lookup URL is not configured");

                    var response = await client.GetAsync(url, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        return (true, "Legacy Employee Service reachable");
                    }
                    else
                    {
                        return (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed || 
                                response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                            ? (true, $"Legacy Employee Service reachable ({(int)response.StatusCode})")
                            : (false, $"Legacy Employee Service returned status {(int)response.StatusCode}");
                    }
                }
            });

            var payload = new
            {
                status = healthy ? "pass" : "fail",
                service = "iLearn.API",
                timestamp = DateTime.UtcNow,
                checks,
            };

            return healthy
                ? Ok(payload)
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
