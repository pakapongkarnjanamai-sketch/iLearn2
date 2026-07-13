using iLearn.Application.Common;

namespace iLearn.Tests
{
    public class ScormDurationParserTests
    {
        // === ToSeconds — SCORM 1.2 timespan format ===

        [Theory]
        [InlineData("00:05:30", 330)]
        [InlineData("0001:00:00", 3600)]
        [InlineData("00:00:30.55", 31)]   // 30 + 0.55 rounds to 31
        [InlineData("00:00:00", 0)]
        [InlineData("0000:00:00", 0)]
        [InlineData("01:30:45", 5445)]
        [InlineData("0010:00:00", 36000)]
        [InlineData("00:01:00.99", 61)]   // 60 + 0.99 rounds to 61
        public void ToSeconds_Scorm12Timespan_ParsesCorrectly(string input, int expected)
        {
            Assert.Equal(expected, ScormDurationParser.ToSeconds(input));
        }

        // === ToSeconds — ISO8601 duration (SCORM 2004) ===

        [Theory]
        [InlineData("PT5M30S", 330)]
        [InlineData("PT1H", 3600)]
        [InlineData("PT30.5S", 31)]       // 30.5 rounds to 31
        [InlineData("P1DT2H", 93600)]     // 86400 + 7200
        [InlineData("PT1H1M1S", 3661)]
        [InlineData("PT0S", 0)]
        [InlineData("PT2H30M", 9000)]
        [InlineData("PT45S", 45)]
        [InlineData("P0D", 0)]
        public void ToSeconds_Iso8601Duration_ParsesCorrectly(string input, int expected)
        {
            Assert.Equal(expected, ScormDurationParser.ToSeconds(input));
        }

        // === ToSeconds — Edge cases ===

        [Theory]
        [InlineData(null, 0)]
        [InlineData("", 0)]
        [InlineData("   ", 0)]
        [InlineData("garbage", 0)]
        [InlineData("PT", 0)]
        [InlineData("not:a:time:value", 0)]
        [InlineData("12:ab:00", 0)]
        public void ToSeconds_InvalidInput_ReturnsZero(string? input, int expected)
        {
            Assert.Equal(expected, ScormDurationParser.ToSeconds(input));
        }

        // === FromSeconds — SCORM 1.2 ===

        [Theory]
        [InlineData(3661, "0001:01:01")]
        [InlineData(0, "0000:00:00")]
        [InlineData(3600, "0001:00:00")]
        [InlineData(60, "0000:01:00")]
        [InlineData(1, "0000:00:01")]
        [InlineData(36000, "0010:00:00")]
        public void FromSeconds_Scorm12_FormatsCorrectly(int seconds, string expected)
        {
            Assert.Equal(expected, ScormDurationParser.FromSeconds(seconds, "1.2"));
        }

        // === FromSeconds — SCORM 2004 ===

        [Theory]
        [InlineData(3661, "PT1H1M1S")]
        [InlineData(0, "PT0S")]
        [InlineData(3600, "PT1H")]
        [InlineData(60, "PT1M")]
        [InlineData(45, "PT45S")]
        [InlineData(93600, "PT26H")]      // 26 hours = 93600s
        public void FromSeconds_Scorm2004_FormatsCorrectly(int seconds, string expected)
        {
            Assert.Equal(expected, ScormDurationParser.FromSeconds(seconds, "2004"));
        }

        // === Roundtrip ===

        [Theory]
        [InlineData(0, "1.2")]
        [InlineData(330, "1.2")]
        [InlineData(3661, "1.2")]
        [InlineData(0, "2004")]
        [InlineData(330, "2004")]
        [InlineData(3661, "2004")]
        public void FromSeconds_ThenToSeconds_Roundtrips(int seconds, string version)
        {
            var formatted = ScormDurationParser.FromSeconds(seconds, version);
            var parsed = ScormDurationParser.ToSeconds(formatted);
            Assert.Equal(seconds, parsed);
        }
    }
}
