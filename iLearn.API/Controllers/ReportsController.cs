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

        [HttpGet("assignments")]
        public async Task<IActionResult> GetAssignments(CancellationToken cancellationToken)
        {
            var result = await _reportService.GetAssignmentSummaryReportAsync(
                _currentUser.DivisionId, _dateTime.Now, cancellationToken);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("assignments/export")]
        public async Task<IActionResult> ExportAssignments(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? lang,
            CancellationToken cancellationToken)
        {
            var now = _dateTime.Now;
            var bytes = await _reportService.BuildAssignmentReportExcelAsync(
                _currentUser.DivisionId, from, to, lang, now, cancellationToken);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"assignment-report-{now:yyyyMMdd-HHmm}.xlsx");
        }

        [HttpGet("learner-groups")]
        public async Task<IActionResult> GetLearnerGroups(CancellationToken cancellationToken)
        {
            var result = await _reportService.GetLearnerGroupSummaryReportAsync(
                _currentUser.DivisionId, _dateTime.Now, cancellationToken);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("learner-groups/export")]
        public async Task<IActionResult> ExportLearnerGroups(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? lang,
            CancellationToken cancellationToken)
        {
            var now = _dateTime.Now;
            var bytes = await _reportService.BuildLearnerGroupReportExcelAsync(
                _currentUser.DivisionId, from, to, lang, now, cancellationToken);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"learner-group-report-{now:yyyyMMdd-HHmm}.xlsx");
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
