namespace iLearn.Application.Common
{
    public class FileSettings
    {
        public string HostUrl { get; set; } = string.Empty;
        public string HostUnc { get; set; } = string.Empty;
        public string CourseFolder { get; set; } = "course";

        // Helper Properties (???????? PathConst ????)
        public string FileUrl => $"{HostUrl}/{CourseFolder}";
        public string FileUnc => Path.Combine(HostUnc, CourseFolder);
    }
}
