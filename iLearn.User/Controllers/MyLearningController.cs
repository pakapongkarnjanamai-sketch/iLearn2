using iLearn.Application.Common;
using iLearn.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace iLearn.User.Controllers
{
    [Authorize(Policy = "LearnerSession")]
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
            var learnerCode = GetAuthenticatedLearnerCode();
            if (string.IsNullOrWhiteSpace(learnerCode))
                return LearnerSessionExpired();

            try
            {
                var response = await SendLearnerProxyRequestAsync(
                    HttpMethod.Get,
                    "Enrollments/my-courses",
                    learnerCode);
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
        public async Task<IActionResult> GetCourseCatalog()
        {
            var learnerCode = GetAuthenticatedLearnerCode();
            if (string.IsNullOrWhiteSpace(learnerCode))
                return LearnerSessionExpired();

            var relativeUrl = "Enrollments/course-catalog";
            var divisionName = GetAuthenticatedLearnerDivisionName();
            if (!string.IsNullOrWhiteSpace(divisionName))
            {
                relativeUrl += $"?divisionName={Uri.EscapeDataString(divisionName)}";
            }

            try
            {
                var response = await SendLearnerProxyRequestAsync(
                    HttpMethod.Get,
                    relativeUrl,
                    learnerCode);
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
            var learnerCode = GetAuthenticatedLearnerCode();
            if (string.IsNullOrWhiteSpace(learnerCode))
                return LearnerSessionExpired();

            try
            {
                var response = await SendLearnerProxyRequestAsync(
                    HttpMethod.Get,
                    $"Enrollments/player-info/{courseId}",
                    learnerCode);
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
            var learnerCode = GetAuthenticatedLearnerCode();
            if (string.IsNullOrWhiteSpace(learnerCode))
                return LearnerSessionExpired();

            if (input == null)
                return BadRequest(new { success = false, message = "ข้อมูลการบันทึกไม่ถูกต้อง" });

            try
            {
                var response = await SendLearnerProxyRequestAsync(
                    HttpMethod.Post,
                    "LearningLogs/update-progress",
                    learnerCode,
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
            var learnerCode = GetAuthenticatedLearnerCode();
            if (string.IsNullOrWhiteSpace(learnerCode))
                return LearnerSessionExpired();

            if (input == null)
                return BadRequest(new { success = false, message = "ข้อมูล runtime ไม่ถูกต้อง" });

            try
            {
                var response = await SendLearnerProxyRequestAsync(
                    HttpMethod.Post,
                    "LearningLogs/commit-runtime",
                    learnerCode,
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
            var learnerCode = GetAuthenticatedLearnerCode();
            if (string.IsNullOrWhiteSpace(learnerCode))
                return LearnerSessionExpired();

            if (input == null || input.EnrollmentId <= 0)
                return BadRequest(new { success = false, message = "ข้อมูลการรีเซ็ตไม่ถูกต้อง" });

            try
            {
                var response = await SendLearnerProxyRequestAsync(
                    HttpMethod.Post,
                    "LearningLogs/reset-progress",
                    learnerCode,
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

        private string? GetAuthenticatedLearnerCode()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private string? GetAuthenticatedLearnerDivisionName()
        {
            var division = User.FindFirst("Division")?.Value;
            if (string.IsNullOrWhiteSpace(division) || division == "-")
            {
                return null;
            }

            return division;
        }

        private IActionResult LearnerSessionExpired()
        {
            return StatusCode(440, new
            {
                success = false,
                sessionExpired = true,
                message = "ไม่พบข้อมูลผู้เรียนใน session ปัจจุบัน กรุณาเข้าสู่ระบบอีกครั้ง",
                redirectUrl = Url.Action("Index", "Home")
            });
        }

        private async Task<HttpResponseMessage> SendLearnerProxyRequestAsync(
            HttpMethod method,
            string relativeUrl,
            string learnerCode,
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
                learnerCode,
                timestamp,
                method.Method,
                absoluteUri.AbsolutePath);

            using var request = new HttpRequestMessage(method, absoluteUri);
            request.Headers.TryAddWithoutValidation(LearnerProxyAuthHeaders.LearnerCode, learnerCode);
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

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new ObjectResult(new
                {
                    success = false,
                    message = "ไม่สามารถยืนยันสิทธิ์กับระบบบริการได้ กรุณาลองใหม่อีกครั้ง"
                })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
            }

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = content,
                ContentType = mediaType
            };
        }
    }
}