using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;

        public ReportsController(
            IReportService reportService,
            ICurrentUserService currentUser,
            IDateTime dateTime)
        {
            _reportService = reportService;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        [HttpGet("compliance")]
        public async Task<IActionResult> GetCompliance(CancellationToken cancellationToken)
        {
            var result = await _reportService.GetComplianceReportAsync(
                _currentUser.DivisionId, _dateTime.Now, cancellationToken);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("transcript/{learnerCode}")]
        public async Task<IActionResult> GetTranscript(string learnerCode, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _reportService.GetTranscriptReportAsync(
                    learnerCode, _currentUser.DivisionId, _dateTime.Now, cancellationToken);
                return Ok(new { success = true, data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("course-summary")]
        public async Task<IActionResult> GetCourseSummary(CancellationToken cancellationToken)
        {
            var result = await _reportService.GetCourseSummaryReportAsync(
                _currentUser.DivisionId, _dateTime.Now, cancellationToken);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("activity")]
        public async Task<IActionResult> GetActivity([FromQuery] int months = 12, CancellationToken cancellationToken = default)
        {
            var clampedMonths = Math.Clamp(months, 3, 24);
            var result = await _reportService.GetActivityReportAsync(
                clampedMonths, _currentUser.DivisionId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
    }
}
