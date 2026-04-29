using iLearn.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace iLearn.Application.Common
{
    public sealed record ContentItemReadinessIssue(int ContentItemId, string ContentItemName, string Reason);

    public static class CourseContentReadiness
    {
        public static bool IsContentItemReady(ContentItem? contentItem)
        {
            return GetContentItemIssue(contentItem) == null;
        }

        public static ContentItemReadinessIssue? GetContentItemIssue(ContentItem? contentItem, int contentItemId = 0)
        {
            if (contentItem == null)
            {
                return new ContentItemReadinessIssue(contentItemId, $"Content item {contentItemId}", "content item record is missing");
            }

            if (!contentItem.IsActive)
            {
                return new ContentItemReadinessIssue(contentItem.Id, contentItem.Name, "content item is not published");
            }

            if (string.IsNullOrWhiteSpace(contentItem.URL))
            {
                return new ContentItemReadinessIssue(contentItem.Id, contentItem.Name, "launch URL is missing");
            }

            if (string.IsNullOrWhiteSpace(contentItem.LaunchHref) && !LooksLikeDirectLaunchUrl(contentItem.URL))
            {
                return new ContentItemReadinessIssue(contentItem.Id, contentItem.Name, "SCORM launch file is missing");
            }

            return null;
        }

        public static bool IsVersionReady(IEnumerable<CourseContentItem>? courseContentItems)
        {
            if (courseContentItems == null)
            {
                return false;
            }

            var contentItems = courseContentItems.ToList();
            return contentItems.Count > 0 && contentItems.All(cr => IsContentItemReady(cr.ContentItem));
        }

        public static bool HasReadyActiveVersion(Course? course)
        {
            return course?.IsActive == true
                && course.Versions.Any(version => version.IsActive && IsVersionReady(version.CourseContentItems));
        }

        public static string BuildActivationErrorMessage(int contentItemCount, IReadOnlyCollection<ContentItemReadinessIssue> issues)
        {
            if (contentItemCount == 0)
            {
                return "Cannot activate this course version because it has no content items.";
            }

            var detail = string.Join("; ", issues.Take(5).Select(issue => $"{issue.ContentItemName}: {issue.Reason}"));
            if (issues.Count > 5)
            {
                detail += $"; and {issues.Count - 5} more content item(s)";
            }

            return $"Cannot activate this course version because its content items are not ready. {detail}";
        }

        private static bool LooksLikeDirectLaunchUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _)
                || url.StartsWith("/", StringComparison.Ordinal)
                || url.Contains('/', StringComparison.Ordinal)
                || url.Contains('\\', StringComparison.Ordinal)
                || Path.HasExtension(url);
        }
    }
}