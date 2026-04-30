using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    public interface IContentPublicationService
    {
        Task<ContentItemDto> PublishAsync(int contentItemId);
        Task<ContentItemDto> UnpublishAsync(int contentItemId);
        Task<ContentUnpublishImpactPreviewDto> PreviewBatchUnpublishAsync(IEnumerable<int> contentItemIds);
    }
}