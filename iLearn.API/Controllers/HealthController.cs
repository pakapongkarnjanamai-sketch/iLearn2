using System.Diagnostics;
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

        public HealthController(AppDbContext db, IOptions<FileSettings> fileSettings)
        {
            _db = db;
            _fileSettings = fileSettings.Value;
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
