using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.Common;
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
    public class CourseVersionsCRUDController : GenericController<CourseVersion>
    {
        public CourseVersionsCRUDController(
            IGenericRepository<CourseVersion> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }

        [HttpGet("Get/{id}")]
        public override Task<IActionResult> Get(int id)
        {
            var entity = _repository.GetQuery()
                .Include(c => c.Course!).ThenInclude(course => course.Category)
                .Include(cr => cr.CourseContentItems).ThenInclude(c => c.ContentItem)
                .FirstOrDefault(i => i.Id == id);

            if (entity == null)
            {
                return Task.FromResult<IActionResult>(NotFound());
            }

            return Task.FromResult<IActionResult>(Ok(ToDto(entity)));
        }

        private static CourseVersionDto ToDto(CourseVersion entity)
        {
            var sortedContentItems = entity.CourseContentItems
                .OrderBy(courseContentItem => courseContentItem.Order ?? int.MaxValue)
                .ThenBy(courseContentItem => courseContentItem.ContentItem?.Name)
                .ToList();

            return new CourseVersionDto
            {
                Id = entity.Id,
                CourseId = entity.CourseId,
                VersionNumber = entity.VersionNumber,
                Note = entity.Note ?? string.Empty,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                ContentItems = sortedContentItems.Select(courseContentItem => new CourseContentItemDto
                {
                    Id = courseContentItem.ContentItem?.Id ?? 0,
                    Name = courseContentItem.ContentItem?.Name ?? "Unknown",
                    TypeId = courseContentItem.ContentItem?.TypeId ?? 0,
                    TypeName = courseContentItem.ContentItem?.TypeId == ScormContentStatusPolicy.ExamTypeId ? "Exam" : "Learn",
                    IsActive = courseContentItem.ContentItem?.IsActive ?? false,
                    URL = courseContentItem.ContentItem?.URL
                }).ToList()
            };
        }
    }
}
