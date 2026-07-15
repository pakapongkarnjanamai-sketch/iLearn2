using iLearn.API.Hubs;
using iLearn.API.Services;
using iLearn.Application.Common;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

namespace iLearn.Tests
{
    public class NotificationServiceTests
    {
        private static AppDbContext CreateInMemoryDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            var dateTime = new FakeDateTime();
            var currentUser = new FakeCurrentUser("SYSTEM");
            return new AppDbContext(options, dateTime, currentUser);
        }

        private static NotificationService CreateService(AppDbContext db, IDateTime? dateTime = null)
        {
            var hubContext = new FakeHubContext();
            var logger = NullLogger<NotificationService>.Instance;
            return new NotificationService(db, hubContext, logger, dateTime ?? new FakeDateTime());
        }

        [Fact]
        public async Task GetForUserAsync_ReturnsOnlyCurrentUserNotifications()
        {
            var db = CreateInMemoryDb(nameof(GetForUserAsync_ReturnsOnlyCurrentUserNotifications));
            db.Notifications.AddRange(
                new Notification { Id = 1, RecipientUserId = "n4734", Type = "Test", Level = "info", Title = "Mine", CreatedAt = DateTime.UtcNow },
                new Notification { Id = 2, RecipientUserId = "other", Type = "Test", Level = "info", Title = "Others", CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.GetForUserAsync("n4734", unreadOnly: false, take: 20);

            Assert.Single(result.Items);
            Assert.Equal("Mine", result.Items[0].Title);
            Assert.Equal(1, result.UnreadCount);
        }

        [Fact]
        public async Task GetForUserAsync_OrdersByCreatedAtDesc()
        {
            var db = CreateInMemoryDb(nameof(GetForUserAsync_OrdersByCreatedAtDesc));
            db.Notifications.AddRange(
                new Notification { Id = 1, RecipientUserId = "n4734", Type = "Test", Level = "info", Title = "Old", CreatedAt = new DateTime(2025, 1, 1) },
                new Notification { Id = 2, RecipientUserId = "n4734", Type = "Test", Level = "info", Title = "New", CreatedAt = new DateTime(2025, 6, 1) }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.GetForUserAsync("n4734", unreadOnly: false, take: 20);

            Assert.Equal("New", result.Items[0].Title);
            Assert.Equal("Old", result.Items[1].Title);
        }

        [Fact]
        public async Task GetForUserAsync_TakeClamp()
        {
            var db = CreateInMemoryDb(nameof(GetForUserAsync_TakeClamp));
            for (int i = 1; i <= 5; i++)
            {
                db.Notifications.Add(new Notification { Id = i, RecipientUserId = "n4734", Type = "Test", Level = "info", Title = $"N{i}", CreatedAt = DateTime.UtcNow.AddMinutes(i) });
            }
            await db.SaveChangesAsync();

            var service = CreateService(db);

            // take = 0 should clamp to 1
            var result = await service.GetForUserAsync("n4734", unreadOnly: false, take: 0);
            Assert.Single(result.Items);

            // take = 100 should clamp to 50
            var result2 = await service.GetForUserAsync("n4734", unreadOnly: false, take: 100);
            Assert.Equal(5, result2.Items.Count); // only 5 exist
        }

        [Fact]
        public async Task GetForUserAsync_UnreadOnlyFilter()
        {
            var db = CreateInMemoryDb(nameof(GetForUserAsync_UnreadOnlyFilter));
            db.Notifications.AddRange(
                new Notification { Id = 1, RecipientUserId = "n4734", Type = "Test", Level = "info", Title = "Unread", IsRead = false, CreatedAt = DateTime.UtcNow },
                new Notification { Id = 2, RecipientUserId = "n4734", Type = "Test", Level = "info", Title = "Read", IsRead = true, CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.GetForUserAsync("n4734", unreadOnly: true, take: 20);

            Assert.Single(result.Items);
            Assert.Equal("Unread", result.Items[0].Title);
            // UnreadCount is still total unread for user (not just returned items)
            Assert.Equal(1, result.UnreadCount);
        }

        [Fact]
        public async Task MarkReadAsync_ThrowsForOtherUser()
        {
            var db = CreateInMemoryDb(nameof(MarkReadAsync_ThrowsForOtherUser));
            db.Notifications.Add(new Notification { Id = 1, RecipientUserId = "other", Type = "Test", Level = "info", Title = "X", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.MarkReadAsync("n4734", 1));
        }

        [Fact]
        public async Task MarkReadAsync_MarksAndReturnsNewCount()
        {
            var db = CreateInMemoryDb(nameof(MarkReadAsync_MarksAndReturnsNewCount));
            db.Notifications.AddRange(
                new Notification { Id = 1, RecipientUserId = "n4734", Type = "Test", Level = "info", Title = "A", IsRead = false, CreatedAt = DateTime.UtcNow },
                new Notification { Id = 2, RecipientUserId = "n4734", Type = "Test", Level = "info", Title = "B", IsRead = false, CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var newCount = await service.MarkReadAsync("n4734", 1);

            Assert.Equal(1, newCount);
            var marked = await db.Notifications.FindAsync(1);
            Assert.True(marked!.IsRead);
            Assert.NotNull(marked.ReadAt);
        }

        [Fact]
        public async Task MarkAllReadAsync_MarksAllAndDoesNotAffectOtherUser()
        {
            var db = CreateInMemoryDb(nameof(MarkAllReadAsync_MarksAllAndDoesNotAffectOtherUser));
            db.Notifications.AddRange(
                new Notification { Id = 1, RecipientUserId = "n4734", Type = "Test", Level = "info", Title = "A", IsRead = false, CreatedAt = DateTime.UtcNow },
                new Notification { Id = 2, RecipientUserId = "n4734", Type = "Test", Level = "info", Title = "B", IsRead = false, CreatedAt = DateTime.UtcNow },
                new Notification { Id = 3, RecipientUserId = "other", Type = "Test", Level = "info", Title = "C", IsRead = false, CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var newCount = await service.MarkAllReadAsync("n4734");

            Assert.Equal(0, newCount);

            var otherNotif = await db.Notifications.FindAsync(3);
            Assert.False(otherNotif!.IsRead);
        }

        [Fact]
        public async Task MarkReadAsync_SetsReadAtFromIDateTime_NotRawUtc()
        {
            // Regression: ReadAt must come from IDateTime.Now (UTC+7 in production, same source
            // SaveChanges uses for CreatedAt). Raw DateTime.UtcNow would land 7h behind CreatedAt,
            // making rows look like they were read before they were created.
            var fixedNow = new DateTime(2026, 7, 15, 17, 30, 0);
            var db = CreateInMemoryDb(nameof(MarkReadAsync_SetsReadAtFromIDateTime_NotRawUtc));
            db.Notifications.Add(new Notification
            {
                Id = 1,
                RecipientUserId = "n4734",
                Type = "Test",
                Level = "info",
                Title = "Unread",
                IsRead = false
            });
            await db.SaveChangesAsync();

            var service = CreateService(db, new FakeDateTime(fixedNow));
            await service.MarkReadAsync("n4734", 1);

            var saved = await db.Notifications.FirstAsync(n => n.Id == 1);
            Assert.True(saved.IsRead);
            Assert.Equal(fixedNow, saved.ReadAt);
        }

        [Fact]
        public async Task MarkAllReadAsync_SetsReadAtFromIDateTime()
        {
            var fixedNow = new DateTime(2026, 7, 15, 17, 30, 0);
            var db = CreateInMemoryDb(nameof(MarkAllReadAsync_SetsReadAtFromIDateTime));
            db.Notifications.AddRange(
                new Notification { Id = 1, RecipientUserId = "n4734", Type = "Test", Level = "info", Title = "A", IsRead = false },
                new Notification { Id = 2, RecipientUserId = "n4734", Type = "Test", Level = "info", Title = "B", IsRead = false }
            );
            await db.SaveChangesAsync();

            var service = CreateService(db, new FakeDateTime(fixedNow));
            await service.MarkAllReadAsync("n4734");

            var saved = await db.Notifications.Where(n => n.RecipientUserId == "n4734").ToListAsync();
            Assert.All(saved, n => Assert.Equal(fixedNow, n.ReadAt));
        }

        [Fact]
        public async Task NotifyAsync_DoesNotThrowOnHubFailure()
        {
            var db = CreateInMemoryDb(nameof(NotifyAsync_DoesNotThrowOnHubFailure));
            var hubContext = new FakeHubContext(throwOnSend: true);
            var logger = NullLogger<NotificationService>.Instance;
            var service = new NotificationService(db, hubContext, logger, new FakeDateTime());

            // Should not throw — swallows the exception
            await service.NotifyAsync("n4734", NotificationTypes.ScormUploadSucceeded, NotificationLevels.Success, "Test");

            // Notification was still persisted before hub failure
            var count = await db.Notifications.CountAsync();
            Assert.Equal(1, count);
        }

        // ── Fakes ──

        private sealed class FakeDateTime : IDateTime
        {
            private readonly DateTime? _fixedNow;

            // Default (no arg) keeps advancing so CreatedAt ordering stays realistic;
            // pass a fixed value when a test needs to assert an exact timestamp.
            public FakeDateTime(DateTime? fixedNow = null) => _fixedNow = fixedNow;

            public DateTime Now => _fixedNow ?? DateTime.UtcNow;
            public CultureInfo CultureInfo => CultureInfo.InvariantCulture;
            public DateTime UnixTime => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        private sealed class FakeCurrentUser : ICurrentUserService
        {
            public FakeCurrentUser(string userId) => UserId = userId;
            public string UserId { get; }
            public string FullName => UserId;
            public bool IsAuthenticated => true;
            public int? DivisionId => null;
            public string? DivisionName => null;
            public bool IsSuperAdmin => true;
        }

        private sealed class FakeHubContext : IHubContext<AdminActivityHub>
        {
            private readonly bool _throwOnSend;
            public FakeHubContext(bool throwOnSend = false) => _throwOnSend = throwOnSend;

            public IHubClients Clients => new FakeHubClients(_throwOnSend);
            public IGroupManager Groups => throw new NotImplementedException();

            private sealed class FakeHubClients : IHubClients
            {
                private readonly bool _throwOnSend;
                public FakeHubClients(bool throwOnSend) => _throwOnSend = throwOnSend;

                public IClientProxy All => new FakeClientProxy(_throwOnSend);
                public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => new FakeClientProxy(_throwOnSend);
                public IClientProxy Client(string connectionId) => new FakeClientProxy(_throwOnSend);
                public IClientProxy Clients(IReadOnlyList<string> connectionIds) => new FakeClientProxy(_throwOnSend);
                public IClientProxy Group(string groupName) => new FakeClientProxy(_throwOnSend);
                public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => new FakeClientProxy(_throwOnSend);
                public IClientProxy Groups(IReadOnlyList<string> groupNames) => new FakeClientProxy(_throwOnSend);
                public IClientProxy User(string userId) => new FakeClientProxy(_throwOnSend);
                public IClientProxy Users(IReadOnlyList<string> userIds) => new FakeClientProxy(_throwOnSend);
            }

            private sealed class FakeClientProxy : IClientProxy
            {
                private readonly bool _throwOnSend;
                public FakeClientProxy(bool throwOnSend) => _throwOnSend = throwOnSend;

                public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
                {
                    if (_throwOnSend)
                        throw new InvalidOperationException("Simulated hub failure");
                    return Task.CompletedTask;
                }
            }
        }
    }
}
