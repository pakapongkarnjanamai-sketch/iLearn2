using iLearn.Application.Common;
using Xunit;

namespace iLearn.Tests
{
    public class NameHelperTests
    {
        [Theory]
        [InlineData("นาย สมชาย เข็มกลัด", "สมชาย เข็มกลัด")]
        [InlineData("นายสมชาย เข็มกลัด", "สมชาย เข็มกลัด")]
        [InlineData("นางสาว สมหญ้า สดใส", "สมหญ้า สดใส")]
        [InlineData("นางสาวสมหญ้า สดใส", "สมหญ้า สดใส")]
        [InlineData("น.ส. สมหญ้า สดใส", "สมหญ้า สดใส")]
        [InlineData("น.ส.สมหญ้า สดใส", "สมหญ้า สดใส")]
        [InlineData("นาง สมศรี มีสุข", "สมศรี มีสุข")]
        [InlineData("เด็กชาย สมศักดิ์", "สมศักดิ์")]
        [InlineData("ด.ช. สมศักดิ์", "สมศักดิ์")]
        [InlineData("เด็กหญิง สมจิตร", "สมจิตร")]
        [InlineData("ด.ญ. สมจิตร", "สมจิตร")]
        [InlineData("Mr. John Doe", "John Doe")]
        [InlineData("Mr John Doe", "John Doe")]
        [InlineData("Mrs. Jane Doe", "Jane Doe")]
        [InlineData("Miss Jack Doe", "Jack Doe")]
        [InlineData("Ms. Jill Doe", "Jill Doe")]
        [InlineData("Master Bob", "Bob")]
        [InlineData("Somchai Khemglad", "Somchai Khemglad")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void StripGenderPrefix_RemovesGenderPrefixesCorrectly(string? input, string expected)
        {
            var result = NameHelper.StripGenderPrefix(input);
            Assert.Equal(expected, result);
        }
    }
}
