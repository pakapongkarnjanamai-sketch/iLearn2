using iLearn.Application.DTOs;
using iLearn.Domain.Enums;

namespace iLearn.Tests
{
    public class LifecycleContractDtoTests
    {
        [Theory]
        [InlineData(true, true, "Published")]
        [InlineData(false, false, "Unpublished")]
        public void ContentItemDto_ExposesPublishState(bool isActive, bool expectedPublished, string expectedState)
        {
            var dto = new ContentItemDto
            {
                IsActive = isActive
            };

            Assert.Equal(expectedPublished, dto.IsPublished);
            Assert.Equal(expectedState, dto.PublishState);
        }

        [Theory]
        [InlineData(true, true, "Published")]
        [InlineData(false, false, "Unpublished")]
        public void CourseContentItemDto_ExposesPublishState(bool isActive, bool expectedPublished, string expectedState)
        {
            var dto = new CourseContentItemDto
            {
                IsActive = isActive
            };

            Assert.Equal(expectedPublished, dto.IsPublished);
            Assert.Equal(expectedState, dto.PublishState);
        }

        [Theory]
        [InlineData(true, "Active")]
        [InlineData(false, "Inactive")]
        public void CourseVersionDto_ExposesVersionState(bool isActive, string expectedState)
        {
            var dto = new CourseVersionDto
            {
                IsActive = isActive
            };

            Assert.Equal(expectedState, dto.VersionState);
        }

        [Fact]
        public void CourseDto_ExposesCourseLifecycleSemantics()
        {
            var dto = new CourseDto
            {
                Status = CourseStatus.Open,
                CanAssign = true,
                CanLearnerAccess = true
            };

            Assert.Equal("Open", dto.StatusName);
            Assert.True(dto.CanAssign);
            Assert.True(dto.CanLearnerAccess);
        }

        [Fact]
        public void CourseDetailDto_ExposesCourseLifecycleSemantics()
        {
            var dto = new CourseDetailDto
            {
                Status = CourseStatus.Closed,
                CanAssign = false,
                CanLearnerAccess = true
            };

            Assert.Equal("Closed", dto.StatusName);
            Assert.False(dto.CanAssign);
            Assert.True(dto.CanLearnerAccess);
        }

        [Fact]
        public void CourseStatusResultDto_ExposesCourseLifecycleSemantics()
        {
            var dto = new CourseStatusResultDto
            {
                Status = CourseStatus.Closed,
                CanAssign = false,
                CanLearnerAccess = true
            };

            Assert.Equal("Closed", dto.StatusName);
            Assert.False(dto.CanAssign);
            Assert.True(dto.CanLearnerAccess);
        }
    }
}