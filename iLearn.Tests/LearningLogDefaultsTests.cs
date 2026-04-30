using iLearn.Domain.Entities;

namespace iLearn.Tests
{
    public sealed class LearningLogDefaultsTests
    {
        [Fact]
        public void NewLearningLog_UsesSafeIncompleteDefaults()
        {
            var log = new LearningLog();

            Assert.Equal("incomplete", log.Status);
            Assert.Equal(0, log.Progress);
        }
    }
}