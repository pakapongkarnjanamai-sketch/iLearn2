using iLearn.Application.Common;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Enums;
using iLearn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace iLearn.API.Services
{
    public sealed class DeadlineDigestService : IDeadlineDigestService
    {
        private const int RetentionBatchSize = 500;

        private readonly AppDbContext _db;
        private readonly IDateTime _dateTime;
        private readonly INotificationService _notificationService;
        private readonly ILogger<DeadlineDigestService> _logger;

        public DeadlineDigestService(
            AppDbContext db,
            IDateTime dateTime,
            INotificationService notificationService,
            ILogger<DeadlineDigestService> logger)
        {
            _db = db;
            _dateTime = dateTime;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<int> RunOnceAsync(CancellationToken ct = default)
        {
            var now = _dateTime.Now;
            var digestAlreadyCreated = await _db.Notifications
                .AnyAsync(notification =>
                    notification.Type == NotificationTypes.DeadlineDigest &&
                    notification.CreatedAt >= now.Date,
                    ct);

            if (digestAlreadyCreated)
            {
                await PurgeExpiredNotificationsAsync(now, ct);
                return 0;
            }

            var assignments = await GetQualifyingAssignmentsAsync(now, ct);
            var createdCount = 0;

            if (assignments.Count > 0)
            {
                var recipients = await GetRecipientsAsync(ct);

                foreach (var superAdminUserId in recipients.SuperAdminUserIds)
                {
                    createdCount += await CreateDigestAsync(superAdminUserId, assignments);
                }

                foreach (var recipient in recipients.DivisionIdsByUserId)
                {
                    var visibleAssignments = assignments
                        .Where(assignment =>
                            assignment.DivisionId.HasValue &&
                            recipient.Value.Contains(assignment.DivisionId.Value))
                        .ToList();

                    createdCount += await CreateDigestAsync(recipient.Key, visibleAssignments);
                }
            }

            await PurgeExpiredNotificationsAsync(now, ct);
            return createdCount;
        }

        private async Task<List<DigestAssignment>> GetQualifyingAssignmentsAsync(DateTime now, CancellationToken ct)
        {
            var overdueLinks =
                from link in _db.EnrollmentAssignments.AsNoTracking()
                join enrollment in _db.Enrollments.AsNoTracking()
                    on link.EnrollmentId equals enrollment.Id
                where !link.SnapshotCompleted &&
                      !enrollment.IsCompleted &&
                      link.DueDate.HasValue &&
                      link.DueDate.Value < now
                select new
                {
                    link.AssignmentId,
                    link.EnrollmentId
                };

            var rows = await (
                from assignment in _db.Assignments.AsNoTracking()
                where assignment.DueDate.HasValue
                join overdueLink in overdueLinks
                    on assignment.Id equals overdueLink.AssignmentId into assignmentOverdueLinks
                from overdueLink in assignmentOverdueLinks.DefaultIfEmpty()
                select new
                {
                    assignment.Id,
                    assignment.DivisionId,
                    DueDate = assignment.DueDate!.Value,
                    OverdueEnrollmentId = overdueLink == null
                        ? (int?)null
                        : overdueLink.EnrollmentId
                })
                .ToListAsync(ct);

            return rows
                .GroupBy(row => new { row.Id, row.DivisionId, row.DueDate })
                .Select(group => new DigestAssignment(
                    group.Key.DivisionId,
                    AssignmentStatusKeys.IsDueSoon(false, group.Key.DueDate, now),
                    group.Where(row => row.OverdueEnrollmentId.HasValue)
                        .Select(row => row.OverdueEnrollmentId!.Value)
                        .Distinct()
                        .ToHashSet()))
                .Where(assignment => assignment.IsDueSoon || assignment.OverdueEnrollmentIds.Count > 0)
                .ToList();
        }

        private async Task<DigestRecipients> GetRecipientsAsync(CancellationToken ct)
        {
            var roleAssignments = await (
                from userRole in _db.UserRoles.AsNoTracking()
                join user in _db.Users.AsNoTracking()
                    on userRole.UserId equals user.Id
                join role in _db.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where !string.IsNullOrWhiteSpace(user.Nid)
                select new
                {
                    user.Nid,
                    role.DivisionId,
                    role.RoleType
                })
                .ToListAsync(ct);

            var superAdminUserIds = roleAssignments
                .Where(role => role.RoleType == RoleType.SuperAdmin)
                .Select(role => role.Nid)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var divisionIdsByUserId = roleAssignments
                .Where(role =>
                    role.RoleType == RoleType.Admin &&
                    role.DivisionId.HasValue &&
                    !superAdminUserIds.Contains(role.Nid))
                .GroupBy(role => role.Nid, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(role => role.DivisionId!.Value)
                        .ToHashSet(),
                    StringComparer.OrdinalIgnoreCase);

            return new DigestRecipients(superAdminUserIds, divisionIdsByUserId);
        }

        private async Task<int> CreateDigestAsync(string recipientUserId, IReadOnlyCollection<DigestAssignment> assignments)
        {
            if (assignments.Count == 0)
            {
                return 0;
            }

            var dueSoonAssignmentCount = assignments.Count(assignment => assignment.IsDueSoon);
            var overdueAssignments = assignments
                .Where(assignment => assignment.OverdueEnrollmentIds.Count > 0)
                .ToList();
            var overdueLearnerCount = overdueAssignments
                .SelectMany(assignment => assignment.OverdueEnrollmentIds)
                .Distinct()
                .Count();

            var message = $"ครบกำหนดใน 7 วัน: {dueSoonAssignmentCount} งาน";
            if (overdueAssignments.Count > 0)
            {
                message += $" · มีผู้เรียนเกินกำหนด: {overdueAssignments.Count} งาน ({overdueLearnerCount} คน)";
            }

            await _notificationService.NotifyAsync(
                recipientUserId,
                NotificationTypes.DeadlineDigest,
                overdueAssignments.Count > 0 ? NotificationLevels.Error : NotificationLevels.Info,
                "สรุปงานใกล้ครบกำหนดประจำวัน",
                message,
                linkPath: "/assignments");

            _logger.LogDebug(
                "Created deadline digest for {RecipientUserId}: {DueSoonAssignmentCount} due soon, {OverdueAssignmentCount} overdue assignments, {OverdueLearnerCount} overdue learners.",
                recipientUserId,
                dueSoonAssignmentCount,
                overdueAssignments.Count,
                overdueLearnerCount);

            return 1;
        }

        private async Task PurgeExpiredNotificationsAsync(DateTime now, CancellationToken ct)
        {
            var cutoff = now.AddDays(-NotificationTypes.NotificationRetentionDays);

            while (true)
            {
                var expiredNotifications = await _db.Notifications
                    .Where(notification => notification.CreatedAt < cutoff)
                    .OrderBy(notification => notification.Id)
                    .Take(RetentionBatchSize)
                    .ToListAsync(ct);

                if (expiredNotifications.Count == 0)
                {
                    return;
                }

                _db.Notifications.RemoveRange(expiredNotifications);
                await _db.SaveChangesAsync(ct);
            }
        }

        private sealed record DigestAssignment(
            int? DivisionId,
            bool IsDueSoon,
            IReadOnlyCollection<int> OverdueEnrollmentIds);

        private sealed record DigestRecipients(
            IReadOnlyCollection<string> SuperAdminUserIds,
            IReadOnlyDictionary<string, HashSet<int>> DivisionIdsByUserId);
    }
}