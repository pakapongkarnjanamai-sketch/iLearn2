using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task NotifyAsync(string recipientUserId, string type, string level, string title,
            string? message = null, string? linkPath = null,
            string? entityType = null, int? entityId = null);

        Task<NotificationListDto> GetForUserAsync(string userId, bool unreadOnly, int take);
        Task<int> GetUnreadCountAsync(string userId);
        Task<int> MarkReadAsync(string userId, int notificationId);
        Task<int> MarkAllReadAsync(string userId);
    }
}
