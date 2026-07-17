using iLearn.API.Hubs;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace iLearn.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<AdminActivityHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly IDateTime _dateTime;

        public NotificationService(
            AppDbContext db,
            IHubContext<AdminActivityHub> hubContext,
            ILogger<NotificationService> logger,
            IDateTime dateTime)
        {
            _db = db;
            _hubContext = hubContext;
            _logger = logger;
            _dateTime = dateTime;
        }

        public async Task NotifyAsync(string recipientUserId, string type, string level, string title,
            string? message = null, string? linkPath = null,
            string? entityType = null, int? entityId = null)
        {
            try
            {
                var notification = new Notification
                {
                    RecipientUserId = recipientUserId,
                    Type = type,
                    Level = level,
                    Title = title,
                    Message = message,
                    LinkPath = linkPath,
                    EntityType = entityType,
                    EntityId = entityId,
                    IsRead = false
                };

                _db.Notifications.Add(notification);
                await _db.SaveChangesAsync();

                var dto = ToDto(notification);
                await _hubContext.Clients.User(recipientUserId).SendAsync("NotificationCreated", dto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create notification for user {UserId}, type {Type}", recipientUserId, type);
            }
        }

        public async Task<NotificationListDto> GetForUserAsync(string userId, bool unreadOnly, int take, int skip = 0)
        {
            take = Math.Clamp(take, 1, 50);
            skip = Math.Max(skip, 0);

            var query = _db.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsDeleted);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    Level = n.Level,
                    Title = n.Title,
                    Message = n.Message,
                    LinkPath = n.LinkPath,
                    EntityType = n.EntityType,
                    EntityId = n.EntityId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            var unreadCount = await _db.Notifications
                .CountAsync(n => n.RecipientUserId == userId && !n.IsDeleted && !n.IsRead);

            return new NotificationListDto
            {
                UnreadCount = unreadCount,
                TotalCount = totalCount,
                Items = items
            };
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _db.Notifications
                .CountAsync(n => n.RecipientUserId == userId && !n.IsDeleted && !n.IsRead);
        }

        public async Task<int> MarkReadAsync(string userId, int notificationId)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId && !n.IsDeleted);

            if (notification == null)
                throw new KeyNotFoundException($"Notification {notificationId} not found for current user.");

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                // IDateTime.Now (UTC+7) — must match CreatedAt, which SaveChanges sets from the
                // same source. Raw DateTime.UtcNow would land 7h behind CreatedAt.
                notification.ReadAt = _dateTime.Now;
                await _db.SaveChangesAsync();
            }

            return await GetUnreadCountAsync(userId);
        }

        public async Task<int> MarkAllReadAsync(string userId)
        {
            var unread = await _db.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsDeleted && !n.IsRead)
                .ToListAsync();

            var now = _dateTime.Now;
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }

            if (unread.Count > 0)
                await _db.SaveChangesAsync();

            return 0;
        }

        private static NotificationDto ToDto(Notification n)
        {
            return new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Level = n.Level,
                Title = n.Title,
                Message = n.Message,
                LinkPath = n.LinkPath,
                EntityType = n.EntityType,
                EntityId = n.EntityId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            };
        }
    }
}
