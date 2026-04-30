using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;

namespace iLearn.Application.Services
{
    public class ContentPublicationService : IContentPublicationService
    {
        private readonly IGenericRepository<ContentItem> _contentItemRepo;
        private readonly IGenericRepository<FileStorage> _fileRepo;
        private readonly IScormService _scormService;

        public ContentPublicationService(
            IGenericRepository<ContentItem> contentItemRepo,
            IGenericRepository<FileStorage> fileRepo,
            IScormService scormService)
        {
            _contentItemRepo = contentItemRepo;
            _fileRepo = fileRepo;
            _scormService = scormService;
        }

        public async Task<ContentItemDto> PublishAsync(int contentItemId)
        {
            var contentItem = await _contentItemRepo.GetByIdAsync(contentItemId);
            if (contentItem == null)
            {
                throw new KeyNotFoundException("Content item not found.");
            }

            if (contentItem.IsActive)
            {
                throw new InvalidOperationException("Content item is already published.");
            }

            var fileStorage = await _fileRepo.GetByIdAsync(contentItem.FileStorageId ?? 0);
            if (fileStorage?.Data == null)
            {
                throw new KeyNotFoundException("Associated file not found.");
            }

            var extension = Path.GetExtension(contentItem.Name).ToLowerInvariant();
            if (extension == ".zip")
            {
                var folderName = Guid.NewGuid().ToString();

                try
                {
                    var scormInfo = await _scormService.ExtractAndParseScormAsync(fileStorage.Data, folderName);
                    contentItem.LaunchHref = scormInfo.LaunchHref;
                    contentItem.SchemaVersion = scormInfo.SchemaVersion;
                    contentItem.URL = scormInfo.FolderName;
                }
                catch
                {
                    _scormService.DeleteScormFolder(folderName);
                    throw;
                }
            }

            contentItem.IsActive = true;
            await _contentItemRepo.UpdateAsync(contentItem);
            return contentItem.ToDto();
        }

        public async Task<ContentItemDto> UnpublishAsync(int contentItemId)
        {
            var contentItem = (await _contentItemRepo.GetAsync(
                r => r.Id == contentItemId,
                includeProperties: "CourseContentItems"))
                .FirstOrDefault();

            if (contentItem == null)
            {
                throw new KeyNotFoundException("Content item not found.");
            }

            if (!contentItem.IsActive)
            {
                throw new InvalidOperationException("Content item is not published.");
            }

            if (contentItem.CourseContentItems.Any())
            {
                throw new InvalidOperationException("Cannot unpublish a content item that is used by courses. Remove it from all course versions first.");
            }

            if (!string.IsNullOrEmpty(contentItem.URL))
            {
                _scormService.DeleteScormFolder(contentItem.URL);
            }

            contentItem.IsActive = false;
            contentItem.URL = null;
            contentItem.LaunchHref = null;
            contentItem.SchemaVersion = null;

            await _contentItemRepo.UpdateAsync(contentItem);
            return contentItem.ToDto();
        }

        public async Task<ContentUnpublishImpactPreviewDto> PreviewBatchUnpublishAsync(IEnumerable<int> contentItemIds)
        {
            var requestedIds = contentItemIds?
                .Distinct()
                .ToList() ?? new List<int>();

            var preview = new ContentUnpublishImpactPreviewDto
            {
                RequestedCount = requestedIds.Count
            };

            if (requestedIds.Count == 0)
            {
                return preview;
            }

            var contentItems = (await _contentItemRepo.GetAsync(
                    r => requestedIds.Contains(r.Id),
                    includeProperties: "CourseContentItems.CourseVersion.Course"))
                .ToDictionary(r => r.Id);

            foreach (var contentItemId in requestedIds)
            {
                if (!contentItems.TryGetValue(contentItemId, out var contentItem))
                {
                    preview.Items.Add(new ContentUnpublishImpactItemDto
                    {
                        ContentItemId = contentItemId,
                        Name = $"Content #{contentItemId}",
                        CanUnpublish = false,
                        BlockingReason = "Content item not found."
                    });
                    continue;
                }

                var linkedCourseCodes = contentItem.CourseContentItems
                    .Where(link => link.CourseVersion != null)
                    .Select(link => link.CourseVersion!.Course?.Code)
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()!;

                if (!contentItem.IsActive)
                {
                    preview.Items.Add(new ContentUnpublishImpactItemDto
                    {
                        ContentItemId = contentItem.Id,
                        Name = contentItem.Name,
                        CanUnpublish = false,
                        BlockingReason = "Content item is not published.",
                        LinkedCourseCodes = linkedCourseCodes
                    });
                    continue;
                }

                if (contentItem.CourseContentItems.Any())
                {
                    preview.Items.Add(new ContentUnpublishImpactItemDto
                    {
                        ContentItemId = contentItem.Id,
                        Name = contentItem.Name,
                        CanUnpublish = false,
                        BlockingReason = "Content item is used by course versions and must be removed from those versions first.",
                        LinkedCourseCodes = linkedCourseCodes
                    });
                    continue;
                }

                preview.EligibleIds.Add(contentItem.Id);
                preview.Items.Add(new ContentUnpublishImpactItemDto
                {
                    ContentItemId = contentItem.Id,
                    Name = contentItem.Name,
                    CanUnpublish = true,
                    LinkedCourseCodes = linkedCourseCodes
                });
            }

            preview.EligibleCount = preview.EligibleIds.Count;
            preview.BlockedCount = preview.RequestedCount - preview.EligibleCount;
            return preview;
        }
    }
}