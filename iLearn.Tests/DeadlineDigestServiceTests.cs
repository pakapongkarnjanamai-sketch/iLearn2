using System.Globalization;
using iLearn.API.Services;
using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using iLearn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace iLearn.Tests
{
    public sealed class DeadlineDigestServiceTests
    {
        [Fact]
        public async Task RunOnceAsync_IsIdempotentForTheSameDay()
        {
            var now = new DateTime(2026, 7, 17, 8, 0, 0);
            var clock = new TestDateTime(now);
            var db = CreateInMemoryDb(nameof(RunOnceAsync_IsIdempotentForTheSameDay), clock);
            AddUserWithRole(db, 1, "admin-a", 1, RoleType.Admin, divisionId: 10);
            db.Assignments.Add(new Assignment
            {
                Id = 100,
                DivisionId = 10,
                DueDate = now.AddDays(3)
            });
            await db.SaveChangesAsync();

            var notifications = new RecordingNotificationService(db);
            var service = CreateService(db, clock, notifications);

            var firstCreatedCount = await service.RunOnceAsync();
            var secondCreatedCount = await service.RunOnceAsync();

            Assert.Equal(1, firstCreatedCount);
            Assert.Equal(0, secondCreatedCount);
            Assert.Single(notifications.Calls);
            Assert.Single(await db.Notifications.ToListAsync());
        }

        [Fact]
        public async Task RunOnceAsync_ScopesDivisionAdminsAndGivesSuperAdminAnOrganizationDigest()
        {
            var now = new DateTime(2026, 7, 17, 8, 0, 0);
            var clock = new TestDateTime(now);
            var db = CreateInMemoryDb(nameof(RunOnceAsync_ScopesDivisionAdminsAndGivesSuperAdminAnOrganizationDigest), clock);
            AddUserWithRole(db, 1, "admin-a", 1, RoleType.Admin, divisionId: 10);
            AddUserWithRole(db, 2, "admin-b", 2, RoleType.Admin, divisionId: 20);
            AddUserWithRole(db, 3, "super-admin", 3, RoleType.SuperAdmin, divisionId: null);
            db.Assignments.AddRange(
                new Assignment { Id = 100, DivisionId = 10, DueDate = now.AddDays(2) },
                new Assignment { Id = 200, DivisionId = null, DueDate = now.AddDays(4) });
            await db.SaveChangesAsync();

            var notifications = new RecordingNotificationService(db);
            var service = CreateService(db, clock, notifications);

            var createdCount = await service.RunOnceAsync();

            Assert.Equal(2, createdCount);
            Assert.Contains(notifications.Calls, call =>
                call.RecipientUserId == "admin-a" &&
                call.Message == "ครบกำหนดใน 7 วัน: 1 งาน");
            Assert.DoesNotContain(notifications.Calls, call => call.RecipientUserId == "admin-b");
            Assert.Contains(notifications.Calls, call =>
                call.RecipientUserId == "super-admin" &&
                call.Message == "ครบกำหนดใน 7 วัน: 2 งาน");
        }

        [Fact]
        public async Task RunOnceAsync_UsesEnrollmentAssignmentDueDateForOverdueEvaluation()
        {
            var now = new DateTime(2026, 7, 17, 8, 0, 0);
            var clock = new TestDateTime(now);
            var db = CreateInMemoryDb(nameof(RunOnceAsync_UsesEnrollmentAssignmentDueDateForOverdueEvaluation), clock);
            AddUserWithRole(db, 1, "admin-a", 1, RoleType.Admin, divisionId: 10);
            db.Assignments.Add(new Assignment
            {
                Id = 100,
                DivisionId = 10,
                DueDate = now.AddDays(30)
            });
            db.Enrollments.Add(new Enrollment
            {
                Id = 500,
                LearnerCode = "610034",
                IsCompleted = false,
                DueDate = now.AddDays(-1)
            });
            db.EnrollmentAssignments.Add(new EnrollmentAssignment
            {
                Id = 600,
                AssignmentId = 100,
                EnrollmentId = 500,
                DueDate = now.AddDays(1),
                SnapshotCompleted = false
            });
            await db.SaveChangesAsync();

            var notifications = new RecordingNotificationService(db);
            var service = CreateService(db, clock, notifications);

            var createdCount = await service.RunOnceAsync();

            Assert.Equal(0, createdCount);
            Assert.Empty(notifications.Calls);
        }

        [Fact]
        public async Task RunOnceAsync_DoesNotCreateAnEmptyDigest()
        {
            var now = new DateTime(2026, 7, 17, 8, 0, 0);
            var clock = new TestDateTime(now);
            var db = CreateInMemoryDb(nameof(RunOnceAsync_DoesNotCreateAnEmptyDigest), clock);
            AddUserWithRole(db, 1, "admin-a", 1, RoleType.Admin, divisionId: 10);
            db.Assignments.Add(new Assignment
            {
                Id = 100,
                DivisionId = 10,
                DueDate = now.AddDays(30)
            });
            await db.SaveChangesAsync();

            var notifications = new RecordingNotificationService(db);
            var service = CreateService(db, clock, notifications);

            var createdCount = await service.RunOnceAsync();

            Assert.Equal(0, createdCount);
            Assert.Empty(notifications.Calls);
        }

        [Fact]
        public async Task RunOnceAsync_HardDeletesNotificationsOlderThanRetentionWindow()
        {
            var now = new DateTime(2026, 7, 17, 8, 0, 0);
            var clock = new TestDateTime(now.AddDays(-91));
            var db = CreateInMemoryDb(nameof(RunOnceAsync_HardDeletesNotificationsOlderThanRetentionWindow), clock);
            db.Notifications.Add(new Notification
            {
                Id = 1,
                RecipientUserId = "admin-a",
                Type = "Old",
                Level = NotificationLevels.Info,
                Title = "Old notification"
            });
            await db.SaveChangesAsync();

            clock.Now = now.AddDays(-89);
            db.Notifications.Add(new Notification
            {
                Id = 2,
                RecipientUserId = "admin-a",
                Type = "Recent",
                Level = NotificationLevels.Info,
                Title = "Recent notification"
            });
            await db.SaveChangesAsync();

            clock.Now = now;
            var notifications = new RecordingNotificationService(db);
            var service = CreateService(db, clock, notifications);

            var createdCount = await service.RunOnceAsync();

            Assert.Equal(0, createdCount);
            Assert.Null(await db.Notifications.FindAsync(1));
            Assert.NotNull(await db.Notifications.FindAsync(2));
        }

        private static AppDbContext CreateInMemoryDb(string databaseName, IDateTime dateTime)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            return new AppDbContext(options, dateTime, new TestCurrentUser());
        }

        private static DeadlineDigestService CreateService(
            AppDbContext db,
            IDateTime dateTime,
            INotificationService notifications)
        {
            return new DeadlineDigestService(
                db,
                dateTime,
                notifications,
                NullLogger<DeadlineDigestService>.Instance);
        }

        private static void AddUserWithRole(
            AppDbContext db,
            int userId,
            string nid,
            int roleId,
            RoleType roleType,
            int? divisionId)
        {
            db.Users.Add(new User { Id = userId, Nid = nid });
            db.Roles.Add(new Role
            {
                Id = roleId,
                Name = $"{nid}-role",
                RoleType = roleType,
                DivisionId = divisionId
            });
            db.UserRoles.Add(new UserRole
            {
                Id = roleId,
                UserId = userId,
                RoleId = roleId
            });
        }

        private sealed class TestDateTime : IDateTime
        {
            public TestDateTime(DateTime now) => Now = now;

            public DateTime Now { get; set; }
            public CultureInfo CultureInfo => CultureInfo.InvariantCulture;
            public DateTime UnixTime => new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        private sealed class TestCurrentUser : ICurrentUserService
        {
            public string UserId => "SYSTEM";
            public string FullName => "SYSTEM";
            public bool IsAuthenticated => true;
            public int? DivisionId => null;
            public string? DivisionName => null;
            public bool IsSuperAdmin => true;
        }

        private sealed class RecordingNotificationService : INotificationService
        {
            private readonly AppDbContext _db;

            public RecordingNotificationService(AppDbContext db) => _db = db;

            public List<NotificationCall> Calls { get; } = new();

            public async Task NotifyAsync(
                string recipientUserId,
                string type,
                string level,
                string title,
                string? message = null,
                string? linkPath = null,
                string? entityType = null,
                int? entityId = null)
            {
                Calls.Add(new NotificationCall(recipientUserId, type, level, title, message, linkPath));
                _db.Notifications.Add(new Notification
                {
                    RecipientUserId = recipientUserId,
                    Type = type,
                    Level = level,
                    Title = title,
                    Message = message,
                    LinkPath = linkPath,
                    EntityType = entityType,
                    EntityId = entityId
                });
                await _db.SaveChangesAsync();
            }

            public Task<NotificationListDto> GetForUserAsync(string userId, bool unreadOnly, int take)
                => Task.FromResult(new NotificationListDto());

            public Task<NotificationListDto> GetForUserAsync(string userId, bool unreadOnly, int take, int skip = 0)
                => Task.FromResult(new NotificationListDto());

            public Task<int> GetUnreadCountAsync(string userId) => Task.FromResult(0);
            public Task<int> MarkReadAsync(string userId, int notificationId) => Task.FromResult(0);
            public Task<int> MarkAllReadAsync(string userId) => Task.FromResult(0);
        }

        private sealed record NotificationCall(
            string RecipientUserId,
            string Type,
            string Level,
            string Title,
            string? Message,
            string? LinkPath);
    }
}