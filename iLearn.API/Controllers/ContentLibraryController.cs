using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iLearn.API.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class ContentLibraryController : ControllerBase
    {
        private readonly IGenericRepository<ContentItem> _contentItemRepo;

        public ContentLibraryController(IGenericRepository<ContentItem> contentItemRepo)
        {
            _contentItemRepo = contentItemRepo;
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> GetLookup(DataSourceLoadOptions loadOptions)
        {
            var query = _contentItemRepo.GetQuery()
                .AsNoTracking()
                .Select(contentItem => new
                {
                    contentItem.Id,
                    contentItem.Name,
                    contentItem.TypeId,
                    TypeName = contentItem.TypeId == 2 ? "Exam" : "Learn",
                    contentItem.IsActive,
                    IsPublished = contentItem.IsActive,
                    PublishState = contentItem.IsActive ? "Published" : "Unpublished",
                    contentItem.URL,
                    contentItem.CreatedAt,
                    CourseIdsCount = contentItem.CourseContentItems
                        .Where(courseContentItem => courseContentItem.CourseVersion != null)
                        .Select(courseContentItem => courseContentItem.CourseVersion!.CourseId)
                        .Distinct()
                        .Count()
                });

            return Ok(await DataSourceLoader.LoadAsync(query, loadOptions));
        }
    }
}