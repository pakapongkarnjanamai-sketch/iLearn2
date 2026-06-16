using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssignmentsController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;
        private readonly IAssignmentDashboardService _dashboardService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;

        public AssignmentsController(
            IAssignmentService assignmentService,
            IAssignmentDashboardService dashboardService,
            ICurrentUserService currentUser,
            IDateTime dateTime)
        {
            _assignmentService = assignmentService;
            _dashboardService = dashboardService;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] PaginationParams p, CancellationToken cancellationToken)
        {
            var response = await _assignmentService.GetHistoryAsync(
                p,
                _currentUser.DivisionId,
                _dateTime.Now,
                cancellationToken);
            return Ok(response);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("gantt")]
        public async Task<IActionResult> GetGanttTasks(CancellationToken cancellationToken)
        {
            var tasks = await _assignmentService.GetGanttTasksAsync(
                _currentUser.DivisionId,
                _dateTime.Now,
                cancellationToken);
            return Ok(tasks);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var assignments = await _assignmentService.GetByCourseAsync(courseId, _currentUser.DivisionId);
            return Ok(assignments);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _assignmentService.DeleteAssignmentAsync(id, _currentUser.DivisionId);

            return NoContent();
        }

        [Authorize(Policy = "DomainUser")]
        [HttpGet("dashboard/{id}")]
        public async Task<IActionResult> GetDashboardData(int id, CancellationToken cancellationToken)
        {
            var result = await _assignmentService.GetDashboardAsync(id, _currentUser.DivisionId, cancellationToken);
            if (result == null) throw new KeyNotFoundException("Assignment not found");
            return Ok(new AssignmentDashboardResponseDto { Success = true, Data = result });
        }

        [Authorize(Policy = "DomainUser")]
        [HttpGet("resolve/{assignmentNo}")]
        public async Task<IActionResult> ResolveByNo(string assignmentNo, CancellationToken cancellationToken)
        {
            var assignmentId = await _assignmentService.ResolveAssignmentIdByNoAsync(assignmentNo, cancellationToken);

            if (!assignmentId.HasValue)
                throw new KeyNotFoundException("Assignment not found");

            return Ok(new AssignmentResolveResponseDto { Success = true, Data = assignmentId.Value });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("reassign-data/{id}")]
        public async Task<IActionResult> GetReassignData(int id, CancellationToken cancellationToken)
        {
            var result = await _assignmentService.GetReassignDataAsync(id, _currentUser.DivisionId, cancellationToken);
            if (result == null)
                throw new KeyNotFoundException("Assignment not found");

            return Ok(new AssignmentReassignDataResponseDto
            {
                Success = true,
                Data = result,
            });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{id}/reset-enrollments")]
        public async Task<IActionResult> ResetEnrollments(int id, [FromBody] ResetEnrollmentsDto dto)
        {
            var response = await _assignmentService.ResetEnrollmentsAsync(id, dto, _currentUser.DivisionId);
            return Ok(response);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("validate-before-assign")]
        public async Task<IActionResult> ValidateBeforeAssign([FromBody] BulkAssignDto dto)
        {
            var accessibleCourses = await _assignmentService.GetAccessibleCoursesAsync(dto.CourseIds, _currentUser.DivisionId);
            if (_assignmentService.HasUnauthorizedCourses(dto.CourseIds, accessibleCourses))
            {
                return Forbid();
            }

            var result = await _dashboardService.ValidateBeforeAssignAsync(dto);
            if (!result.Success)
                throw new ArgumentException(result.Message);

            return Ok(new ValidateBeforeAssignResponseDto
            {
                Success = result.Success,
                InProgressConflicts = result.InProgressConflicts,
                CompletedConflicts = result.CompletedConflicts,
                ResolvedCount = result.ResolvedCount,
            });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{id}/extend-due-date")]
        public async Task<IActionResult> ExtendDueDate(int id, [FromBody] ExtendDueDateDto dto)
        {
            var response = await _assignmentService.ExtendDueDateAsync(id, dto.NewDueDate);
            return Ok(response);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{id}/courses")]
        public async Task<IActionResult> AddCourses(int id, [FromBody] ManageAssignmentCoursesDto dto)
        {
            var response = await _assignmentService.AddCoursesToAssignmentAsync(id, dto, _currentUser.DivisionId);
            return Ok(response);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{id}/courses/{ruleId}")]
        public async Task<IActionResult> RemoveCourse(int id, int ruleId)
        {
            var response = await _assignmentService.RemoveCourseFromAssignmentAsync(id, ruleId, _currentUser.DivisionId);
            return Ok(response);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{id}/learners")]
        public async Task<IActionResult> AddLearners(int id, [FromBody] ManageAssignmentLearnersDto dto)
        {
            var response = await _assignmentService.AddLearnersToAssignmentAsync(id, dto, _currentUser.DivisionId);
            return Ok(response);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{id}/learners/{learnerCode}")]
        public async Task<IActionResult> RemoveLearner(int id, string learnerCode)
        {
            var response = await _assignmentService.RemoveLearnerFromAssignmentAsync(id, learnerCode, _currentUser.DivisionId);
            return Ok(response);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("lookup-courses")]
        public async Task<IActionResult> GetLookupCourses(DataSourceLoadOptions loadOptions)
        {
            var courses = await _assignmentService.GetAccessibleCoursesAsync([], _currentUser.DivisionId, includeCourseType: true);

            var result = courses.Select(c => new LookupCourseDto
            {
                Id           = c.Id,
                Code         = c.Code,
                Title        = c.Title,
                CategoryId   = c.CategoryId,
                DivisionId   = c.Category?.DivisionId,
                CourseTypeId = c.CourseTypeId,
                CourseTypeName = c.CourseType?.Name
            }).AsQueryable();

            return Ok(DataSourceLoader.Load(result, loadOptions));
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("group/{groupId}/history")]
        public async Task<IActionResult> GetGroupHistory(int groupId)
        {
            var history = await _dashboardService.GetGroupHistoryAsync(groupId);
            return Ok(new AssignmentGroupHistoryResponseDto
            {
                Success = true,
                Data = history,
            });
        }

    }
}