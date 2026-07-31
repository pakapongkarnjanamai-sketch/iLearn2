using iLearn.Application.Common;
using iLearn.Domain.Entities;

namespace iLearn.Tests
{
    public sealed class CourseContentReadinessTests
    {
        [Fact]
        public void IsVersionReady_IgnoresSoftDeletedCourseContentLinks()
        {
            var readyContent = new ContentItem
            {
                Id = 10,
                Name = "Ready content",
                IsActive = true,
                URL = "ready-package",
                LaunchHref = "index.html"
            };
            var deletedUnreadyContent = new ContentItem
            {
                Id = 11,
                Name = "Deleted draft",
                IsActive = false
            };

            var result = CourseContentReadiness.IsVersionReady([
                new CourseContentItem { Id = 1, ContentItemId = readyContent.Id, ContentItem = readyContent },
                new CourseContentItem { Id = 2, ContentItemId = deletedUnreadyContent.Id, ContentItem = deletedUnreadyContent, IsDeleted = true }
            ]);

            Assert.True(result);
        }

        [Fact]
        public void IsVersionReady_TreatsActiveLinksToDeletedContentAsNotReady()
        {
            var deletedContent = new ContentItem
            {
                Id = 12,
                Name = "Deleted content",
                IsDeleted = true,
                IsActive = true,
                URL = "deleted-package",
                LaunchHref = "index.html"
            };

            var result = CourseContentReadiness.IsVersionReady([
                new CourseContentItem { Id = 3, ContentItemId = deletedContent.Id, ContentItem = deletedContent }
            ]);

            Assert.False(result);
        }
    }
}