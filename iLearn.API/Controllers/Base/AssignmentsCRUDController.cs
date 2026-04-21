using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace iLearn.API.Controllers.Base
{
    public class AssignmentsCRUDController : GenericController<Assignment>
    {
        private readonly IAssignmentDashboardService _dashboardService;
        private readonly IDateTime _dateTime;

        public AssignmentsCRUDController(
            IGenericRepository<Assignment> repository,
            ICurrentUserService currentUser,
            IAssignmentDashboardService dashboardService,
            IDateTime dateTime) : base(repository, currentUser)
        {
            _dashboardService = dashboardService;
            _dateTime = dateTime;
        }

        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            var divisionId = _currentUser.DivisionId;
            var currentDate = _dateTime.Now;

            var assignmentRows = await _repository.GetQuery()
                .AsNoTracking()
                .Where(a => !divisionId.HasValue || a.DivisionId == divisionId.Value)
                .Select(a => new
                {
                    a.Id,
                    a.AssignmentNo,
                    a.Description,
                    a.EmployeeCodes,
                    a.CourseId,
                    CourseTitle = a.Course != null ? a.Course.Title : null,
                    IsCourseDeleted = a.Course != null && a.Course.IsDeleted,
                    a.StartDate,
                    a.DueDate,
                    a.CreatedBy,
                    a.CreatedAt
                })
                .ToListAsync();

            // Group by AssignmentNo (same pattern as history)
            var grouped = assignmentRows
                .GroupBy(a => string.IsNullOrWhiteSpace(a.AssignmentNo) ? $"assignment:{a.Id}" : a.AssignmentNo!)
                .Select(g =>
                {
                    var first = g.OrderBy(x => x.Id).First();
                    var courseEntries = g
                        .Where(x => x.CourseId.HasValue && x.CourseTitle != null)
                        .Select(x => new { x.CourseId, x.CourseTitle, x.IsCourseDeleted })
                        .DistinctBy(x => x.CourseId)
                        .ToList();

                    var activeCourses = courseEntries.Where(c => !c.IsCourseDeleted).ToList();
                    var deletedCourses = courseEntries.Where(c => c.IsCourseDeleted).ToList();
                    var courseNameParts = activeCourses.Select(c => c.CourseTitle!)
                        .Concat(deletedCourses.Select(c => $"{c.CourseTitle} [Deleted]"));

                    var studentCount = string.IsNullOrWhiteSpace(first.EmployeeCodes)
                        ? 0
                        : first.EmployeeCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;

                    return new
                    {
                        first.Id,
                        AssignmentNo = g.Key,
                        Description = first.Description ?? string.Empty,
                        first.StartDate,
                        first.DueDate,
                        CourseNames = string.Join(", ", courseNameParts),
                        CourseCount = courseEntries.Count,
                        StudentCount = studentCount,
                        CreatedBy = first.CreatedBy ?? string.Empty,
                        first.CreatedAt,
                        HasDeletedCourse = deletedCourses.Count > 0,
                        Status = AssignmentDashboardService.CalculateStatus(
                            studentCount > 0, false, first.StartDate, first.DueDate, currentDate)
                    };
                })
                .ToList();

            return Ok(DataSourceLoader.Load(grouped, loadOptions));
        }
    }
}
