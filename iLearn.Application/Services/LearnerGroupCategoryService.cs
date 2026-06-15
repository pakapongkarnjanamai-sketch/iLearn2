using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.Application.Services
{
    public class LearnerGroupCategoryService : ILearnerGroupCategoryService
    {
        private readonly IGenericRepository<LearnerGroupCategory> _categoryRepo;
        private readonly IGenericRepository<LearnerGroup> _groupRepo;
        private readonly ICurrentUserService _currentUser;

        /// <summary>Maximum nesting depth for the category hierarchy (root = depth 0). 4 visible levels (0..3).</summary>
        public const int MaxDepth = 3;

        public LearnerGroupCategoryService(
            IGenericRepository<LearnerGroupCategory> categoryRepo,
            IGenericRepository<LearnerGroup> groupRepo,
            ICurrentUserService currentUser)
        {
            _categoryRepo = categoryRepo;
            _groupRepo = groupRepo;
            _currentUser = currentUser;
        }

        public async Task<List<LearnerGroupCategoryDto>> GetAllAsync()
        {
            var query = _categoryRepo.GetQuery().AsNoTracking();
            if (_currentUser.DivisionId.HasValue)
                query = query.Where(c => c.DivisionId == _currentUser.DivisionId.Value);

            return await query.Select(c => new LearnerGroupCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                DivisionId = c.DivisionId,
                ParentId = c.ParentId,
                ParentName = c.Parent != null ? c.Parent.Name : null,
                Depth = c.Depth,
                ChildCount = c.Children.Count,
                LearnerGroupCount = c.LearnerGroups.Count,
                CreatedAt = c.CreatedAt,
                CreatedBy = c.CreatedBy
            }).ToListAsync();
        }

        public async Task<LearnerGroupCategoryDetailDto?> GetByIdAsync(int id)
        {
            var categories = await _categoryRepo.GetAsync(
                filter: c => c.Id == id,
                includeProperties: "Parent,Children,LearnerGroups");
            var category = categories.FirstOrDefault();
            if (category == null) return null;

            if (_currentUser.DivisionId.HasValue && category.DivisionId != _currentUser.DivisionId.Value)
                return null;

            var ancestors = await LoadAncestorsAsync(category);

            var childIds = category.Children.Select(c => c.Id).ToList();
            var childCounts = childIds.Count == 0
                ? new Dictionary<int, (int Children, int Groups)>()
                : await _categoryRepo.GetQuery().AsNoTracking()
                    .Where(c => childIds.Contains(c.Id))
                    .Select(c => new { c.Id, ChildCount = c.Children.Count, GroupCount = c.LearnerGroups.Count })
                    .ToDictionaryAsync(x => x.Id, x => (Children: x.ChildCount, Groups: x.GroupCount));

            var children = category.Children.OrderBy(c => c.Name).Select(c =>
            {
                childCounts.TryGetValue(c.Id, out var counts);
                return new LearnerGroupCategoryChildDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ChildCount = counts.Children,
                    LearnerGroupCount = counts.Groups
                };
            }).ToList();

            var groupIds = category.LearnerGroups.Select(g => g.Id).ToList();
            var memberCounts = groupIds.Count == 0
                ? new Dictionary<int, int>()
                : await _groupRepo.GetQuery().AsNoTracking()
                    .Where(g => groupIds.Contains(g.Id))
                    .Select(g => new { g.Id, MemberCount = g.Members.Count })
                    .ToDictionaryAsync(x => x.Id, x => x.MemberCount);

            var groups = category.LearnerGroups.OrderBy(g => g.Name).Select(g => new LearnerGroupCategoryGroupDto
            {
                Id = g.Id,
                Name = g.Name,
                MemberCount = memberCounts.TryGetValue(g.Id, out var m) ? m : 0
            }).ToList();

            return new LearnerGroupCategoryDetailDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentId = category.ParentId,
                ParentName = category.Parent?.Name,
                Depth = category.Depth,
                CreatedAt = category.CreatedAt,
                CreatedBy = category.CreatedBy,
                Ancestors = ancestors,
                Children = children,
                LearnerGroups = groups
            };
        }

        public async Task<LearnerGroupCategoryDto> CreateAsync(CreateLearnerGroupCategoryDto dto)
        {
            var name = NormalizeName(dto.Name);
            var parent = await ValidateParentForCreateAsync(dto.ParentId);

            var divisionId = parent != null ? parent.DivisionId : (_currentUser.IsSuperAdmin ? dto.DivisionId : _currentUser.DivisionId);

            var category = new LearnerGroupCategory
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                DivisionId = divisionId,
                ParentId = parent?.Id,
                Depth = parent == null ? 0 : parent.Depth + 1,
                Path = BuildPath(parent)
            };

            var created = await _categoryRepo.AddAsync(category);

            return new LearnerGroupCategoryDto
            {
                Id = created.Id,
                Name = created.Name,
                Description = created.Description,
                DivisionId = created.DivisionId,
                ParentId = created.ParentId,
                ParentName = parent?.Name,
                Depth = created.Depth,
                ChildCount = 0,
                LearnerGroupCount = 0,
                CreatedAt = created.CreatedAt,
                CreatedBy = created.CreatedBy
            };
        }

        public async Task UpdateAsync(int id, UpdateLearnerGroupCategoryDto dto)
        {
            var category = await _categoryRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"LearnerGroupCategory id={id} not found");

            if (_currentUser.DivisionId.HasValue && category.DivisionId != _currentUser.DivisionId.Value)
                throw new UnauthorizedAccessException("Cannot update a category from another division.");

            // Determine target division
            LearnerGroupCategory? newParent = null;
            if (dto.ParentId.HasValue)
            {
                newParent = await _categoryRepo.GetByIdAsync(dto.ParentId.Value)
                    ?? throw new ArgumentException("Parent category not found.");
            }

            var targetDivisionId = newParent != null
                ? newParent.DivisionId
                : (_currentUser.IsSuperAdmin ? dto.DivisionId : category.DivisionId);

            if (targetDivisionId != category.DivisionId)
            {
                var hasChildren = await _categoryRepo.GetQuery().AsNoTracking().AnyAsync(c => c.ParentId == id);
                var hasGroups = await _groupRepo.GetQuery().AsNoTracking().AnyAsync(g => g.CategoryId == id);
                if (hasChildren || hasGroups)
                {
                    throw new ArgumentException("Cannot change the division of a category that has sub-categories or learner groups.");
                }
            }

            category.Name = NormalizeName(dto.Name);
            category.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            category.DivisionId = targetDivisionId;

            if (category.ParentId != dto.ParentId)
            {
                await ApplyReparentAsync(category, dto.ParentId);
            }

            await _categoryRepo.UpdateAsync(category);
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"LearnerGroupCategory id={id} not found");

            if (_currentUser.DivisionId.HasValue && category.DivisionId != _currentUser.DivisionId.Value)
                throw new UnauthorizedAccessException("Cannot delete a category from another division.");

            var hasChildren = await _categoryRepo.GetQuery().AsNoTracking().AnyAsync(c => c.ParentId == id);
            if (hasChildren)
                throw new ArgumentException("Cannot delete a category that still has sub-categories. Remove the sub-categories first.");

            var hasGroups = await _groupRepo.GetQuery().AsNoTracking().AnyAsync(g => g.CategoryId == id);
            if (hasGroups)
                throw new ArgumentException("Cannot delete a category that still has learner groups. Move or delete the groups first.");

            await _categoryRepo.DeleteAsync(category);
        }

        // ── Hierarchy helpers ─────────────────────────────────────────
        private static string BuildPath(LearnerGroupCategory? parent)
        {
            if (parent == null) return "/";
            var basePath = string.IsNullOrEmpty(parent.Path) ? "/" : parent.Path;
            if (!basePath.EndsWith("/")) basePath += "/";
            return $"{basePath}{parent.Id}/";
        }

        private static string NormalizeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.");
            return name.Trim();
        }

        private async Task<LearnerGroupCategory?> ValidateParentForCreateAsync(int? parentId)
        {
            if (!parentId.HasValue) return null;

            var parent = await _categoryRepo.GetByIdAsync(parentId.Value)
                ?? throw new ArgumentException("Parent category not found.");

            if (_currentUser.DivisionId.HasValue && parent.DivisionId != _currentUser.DivisionId.Value)
                throw new ArgumentException("Parent category must belong to the same division.");

            if (parent.Depth + 1 > MaxDepth)
                throw new ArgumentException($"Maximum hierarchy depth is {MaxDepth + 1} levels.");

            return parent;
        }

        private async Task ApplyReparentAsync(LearnerGroupCategory category, int? newParentId)
        {
            LearnerGroupCategory? newParent = null;

            if (newParentId.HasValue)
            {
                if (newParentId.Value == category.Id)
                    throw new ArgumentException("A category cannot be its own parent.");

                newParent = await _categoryRepo.GetByIdAsync(newParentId.Value)
                    ?? throw new ArgumentException("Parent category not found.");

                if (_currentUser.DivisionId.HasValue && newParent.DivisionId != _currentUser.DivisionId.Value)
                    throw new ArgumentException("Parent category must belong to the same division.");

                var categoryPath = string.IsNullOrEmpty(category.Path) ? "/" : category.Path;
                if (!categoryPath.EndsWith("/")) categoryPath += "/";
                var selfMarker = $"{categoryPath}{category.Id}/";
                var newParentPath = string.IsNullOrEmpty(newParent.Path) ? "/" : newParent.Path;
                if (!newParentPath.EndsWith("/")) newParentPath += "/";
                if (newParent.Id == category.Id || newParentPath.Contains(selfMarker))
                    throw new ArgumentException("Cannot move a category under one of its own descendants.");
            }

            var newDepth = newParent == null ? 0 : newParent.Depth + 1;
            var newPath = BuildPath(newParent);

            var oldPath = string.IsNullOrEmpty(category.Path) ? "/" : category.Path;
            if (!oldPath.EndsWith("/")) oldPath += "/";
            var oldSelfPath = $"{oldPath}{category.Id}/";

            var maxDescendantDepth = await _categoryRepo.GetQuery()
                .AsNoTracking()
                .Where(c => c.Id == category.Id || (c.Path != null && c.Path.StartsWith(oldSelfPath)))
                .Select(c => (int?)c.Depth)
                .MaxAsync() ?? category.Depth;

            var depthDelta = newDepth - category.Depth;
            if (maxDescendantDepth + depthDelta > MaxDepth)
                throw new ArgumentException($"Moving this category would exceed the maximum hierarchy depth ({MaxDepth + 1} levels).");

            var newSelfPath = $"{newPath}{category.Id}/";
            var descendants = await _categoryRepo.GetQuery()
                .Where(c => c.Path != null && c.Path.StartsWith(oldSelfPath))
                .ToListAsync();

            foreach (var descendant in descendants)
            {
                var currentPath = descendant.Path ?? "/";
                descendant.Path = newSelfPath + currentPath.Substring(oldSelfPath.Length);
                descendant.Depth = descendant.Depth + depthDelta;
                _categoryRepo.UpdateWithoutSave(descendant);
            }

            category.ParentId = newParent?.Id;
            category.Depth = newDepth;
            category.Path = newPath;
        }

        private async Task<List<LearnerGroupCategoryAncestorDto>> LoadAncestorsAsync(LearnerGroupCategory category)
        {
            if (string.IsNullOrEmpty(category.Path) || category.Path == "/") return new();

            var ids = category.Path
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var n) ? n : 0)
                .Where(n => n > 0)
                .ToList();

            if (ids.Count == 0) return new();

            var ancestors = await _categoryRepo.GetQuery()
                .AsNoTracking()
                .Where(c => ids.Contains(c.Id))
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return ids
                .Select(id => ancestors.FirstOrDefault(a => a.Id == id))
                .Where(a => a != null)
                .Select(a => new LearnerGroupCategoryAncestorDto { Id = a!.Id, Name = a.Name })
                .ToList();
        }
    }
}
