using iLearn.Application.Common;
using iLearn.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Claims;

namespace iLearn.User.Controllers
{
    [Authorize]
    public class MyLearningController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LearnerProxyAuthOptions _learnerProxyAuthOptions;

        public MyLearningController(
            IHttpClientFactory httpClientFactory,
            IOptions<LearnerProxyAuthOptions> learnerProxyAuthOptions)
        {
            _httpClientFactory = httpClientFactory;
            _learnerProxyAuthOptions = learnerProxyAuthOptions.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        // เปลี่ยนรับ parameter จาก enrollmentId เป็น courseId
        public IActionResult Player(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCourses()
        {
            var studentCode = GetAuthenticatedStudentCode();
            if (string.IsNullOrWhiteSpace(studentCode))
                return Unauthorized(new { success = false, message = "ไม่พบข้อมูลผู้เรียนใน session ปัจจุบัน" });

            try
            {
                var response = await SendLearnerProxyRequestAsync(
                    HttpMethod.Get,
                    "Enrollments/my-courses",
                    studentCode);
                return await CreateProxyResultAsync(response);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    message = "ไม่สามารถเชื่อมต่อระบบได้ กรุณาลองใหม่อีกครั้ง"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPlayerInfo(int courseId)
        {
            var studentCode = GetAuthenticatedStudentCode();
            if (string.IsNullOrWhiteSpace(studentCode))
                return Unauthorized(new { success = false, message = "ไม่พบข้อมูลผู้เรียนใน session ปัจจุบัน" });

            try
            {
                var response = await SendLearnerProxyRequestAsync(
                    HttpMethod.Get,
                    $"Enrollments/player-info/{courseId}",
                    studentCode);
                return await CreateProxyResultAsync(response);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    message = "ไม่สามารถเชื่อมต่อระบบได้ กรุณาลองใหม่อีกครั้ง"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressDto input)
        {
            var studentCode = GetAuthenticatedStudentCode();
            if (string.IsNullOrWhiteSpace(studentCode))
                return Unauthorized(new { success = false, message = "ไม่พบข้อมูลผู้เรียนใน session ปัจจุบัน" });

            if (input == null)
                return BadRequest(new { success = false, message = "ข้อมูลการบันทึกไม่ถูกต้อง" });

            try
            {
                var response = await SendLearnerProxyRequestAsync(
                    HttpMethod.Post,
                    "LearningLogs/update-progress",
                    studentCode,
                    input);
                return await CreateProxyResultAsync(response);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    message = "ไม่สามารถเชื่อมต่อระบบได้ กรุณาลองใหม่อีกครั้ง"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CommitRuntime([FromBody] ScormRuntimeCommitRequestDto input)
        {
            var studentCode = GetAuthenticatedStudentCode();
            if (string.IsNullOrWhiteSpace(studentCode))
                return Unauthorized(new { success = false, message = "ไม่พบข้อมูลผู้เรียนใน session ปัจจุบัน" });

            if (input == null)
                return BadRequest(new { success = false, message = "ข้อมูล runtime ไม่ถูกต้อง" });

            try
            {
                var response = await SendLearnerProxyRequestAsync(
                    HttpMethod.Post,
                    "LearningLogs/commit-runtime",
                    studentCode,
                    input);
                return await CreateProxyResultAsync(response);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    message = "ไม่สามารถเชื่อมต่อระบบได้ กรุณาลองใหม่อีกครั้ง"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetProgress([FromBody] ResetProgressDto input)
        {
            var studentCode = GetAuthenticatedStudentCode();
            if (string.IsNullOrWhiteSpace(studentCode))
                return Unauthorized(new { success = false, message = "ไม่พบข้อมูลผู้เรียนใน session ปัจจุบัน" });

            if (input == null || input.EnrollmentId <= 0)
                return BadRequest(new { success = false, message = "ข้อมูลการรีเซ็ตไม่ถูกต้อง" });

            try
            {
                var response = await SendLearnerProxyRequestAsync(
                    HttpMethod.Post,
                    "LearningLogs/reset-progress",
                    studentCode,
                    input);
                return await CreateProxyResultAsync(response);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    message = "ไม่สามารถเชื่อมต่อระบบได้ กรุณาลองใหม่อีกครั้ง"
                });
            }
        }

        private string? GetAuthenticatedStudentCode()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private async Task<HttpResponseMessage> SendLearnerProxyRequestAsync(
            HttpMethod method,
            string relativeUrl,
            string studentCode,
            object? body = null)
        {
            if (string.IsNullOrWhiteSpace(_learnerProxyAuthOptions.SharedSecret))
                throw new InvalidOperationException("Learner proxy authentication is not configured.");

            var client = _httpClientFactory.CreateClient("iLearnAPI");
            if (client.BaseAddress == null)
                throw new InvalidOperationException("iLearn API base address is not configured.");

            var absoluteUri = new Uri(client.BaseAddress, relativeUrl);
            var timestamp = LearnerProxyAuthSignature.CreateTimestamp(DateTimeOffset.UtcNow);
            var signature = LearnerProxyAuthSignature.Compute(
                _learnerProxyAuthOptions.SharedSecret,
                studentCode,
                timestamp,
                method.Method,
                absoluteUri.AbsolutePath);

            using var request = new HttpRequestMessage(method, absoluteUri);
            request.Headers.TryAddWithoutValidation(LearnerProxyAuthHeaders.StudentCode, studentCode);
            request.Headers.TryAddWithoutValidation(LearnerProxyAuthHeaders.Timestamp, timestamp);
            request.Headers.TryAddWithoutValidation(LearnerProxyAuthHeaders.Signature, signature);

            if (body != null)
            {
                request.Content = JsonContent.Create(body);
            }

            return await client.SendAsync(request);
        }

        private static async Task<IActionResult> CreateProxyResultAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? "application/json";

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = content,
                ContentType = mediaType
            };
        }
    }
}