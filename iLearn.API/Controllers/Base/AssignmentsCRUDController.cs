using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iLearn.API.Controllers.Base
{
    public class AssignmentsCRUDController : GenericController<Assignment>
    {
        private readonly AppDbContext _context;
        private readonly IDateTime _dateTime;

        public AssignmentsCRUDController(
            IGenericRepository<Assignment> repository,
            ICurrentUserService currentUser,
            AppDbContext context,
            IDateTime dateTime) : base(repository, currentUser)
        {
            _context = context;
            _dateTime = dateTime;
        }

        // NOTE: SQL paging via vw_AssignmentList
        //   The IQueryable returned by vw_AssignmentList is passed directly to
        //   DataSourceLoader.Load, which appends WHERE / ORDER BY / OFFSET-FETCH
        //   in SQL.  Status, CourseNames, LearnerCount are all computed inside
        //   the view so no in-memory aggregation is needed.
        //
        //   Search-panel text (courseNames LIKE '%...%') is non-sargable because
        //   CourseNames is a STRING_AGG expression; at ~10K rows this is fine.
        //   If the dataset grows beyond that, add a persisted computed-column
        //   index or a full-text index on the view.
        [HttpGet("Get")]
        public override async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            var divisionId = _currentUser.DivisionId;

            var query = _context.AssignmentList
                .AsNoTracking()
                .Where(r => !divisionId.HasValue || r.DivisionId == divisionId.Value);

            return Ok(DataSourceLoader.Load(query, loadOptions));
        }
    }
}
