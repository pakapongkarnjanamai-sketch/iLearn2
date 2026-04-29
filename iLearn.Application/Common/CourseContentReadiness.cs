using iLearn.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace iLearn.Application.Common
{
    public sealed record ResourceReadinessIssue(int ResourceId, string ResourceName, string Reason);

    public static class CourseContentReadiness
    {
        public static bool IsResourceReady(Resource? resource)
        {
            return GetResourceIssue(resource) == null;
        }

        public static ResourceReadinessIssue? GetResourceIssue(Resource? resource, int resourceId = 0)
        {
            if (resource == null)
            {
                return new ResourceReadinessIssue(resourceId, $"Resource {resourceId}", "resource record is missing");
            }

            if (!resource.IsActive)
            {
                return new ResourceReadinessIssue(resource.Id, resource.Name, "resource is not published");
            }

            if (string.IsNullOrWhiteSpace(resource.URL))
            {
                return new ResourceReadinessIssue(resource.Id, resource.Name, "launch URL is missing");
            }

            if (string.IsNullOrWhiteSpace(resource.ResourceHref) && !LooksLikeDirectLaunchUrl(resource.URL))
            {
                return new ResourceReadinessIssue(resource.Id, resource.Name, "SCORM launch file is missing");
            }

            return null;
        }

        public static bool IsVersionReady(IEnumerable<CourseResource>? courseResources)
        {
            if (courseResources == null)
            {
                return false;
            }

            var resources = courseResources.ToList();
            return resources.Count > 0 && resources.All(cr => IsResourceReady(cr.Resource));
        }

        public static bool HasReadyActiveVersion(Course? course)
        {
            return course?.IsActive == true
                && course.Versions.Any(version => version.IsActive && IsVersionReady(version.CourseResources));
        }

        public static string BuildActivationErrorMessage(int resourceCount, IReadOnlyCollection<ResourceReadinessIssue> issues)
        {
            if (resourceCount == 0)
            {
                return "Cannot activate this course version because it has no learning resources.";
            }

            var detail = string.Join("; ", issues.Take(5).Select(issue => $"{issue.ResourceName}: {issue.Reason}"));
            if (issues.Count > 5)
            {
                detail += $"; and {issues.Count - 5} more resource(s)";
            }

            return $"Cannot activate this course version because its learning resources are not ready. {detail}";
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