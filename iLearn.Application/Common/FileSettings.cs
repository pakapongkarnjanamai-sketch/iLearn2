
namespace iLearn.Application.Common
{
    public class FileSettings
    {
        public string HostUrl { get; set; } = string.Empty;
        public string HostUnc { get; set; } = string.Empty;
        public string CourseFolder { get; set; } = "Courses";

        // Helper Properties
        public string FileUrl => CombineUrl(HostUrl, CourseFolder);
        public string FileUnc => Path.Combine(NormalizeUncRoot(HostUnc), NormalizeRelativeFolder(CourseFolder));

        private static string CombineUrl(string hostUrl, string folder)
        {
            var normalizedHostUrl = (hostUrl ?? string.Empty).Trim().TrimEnd('/', '\\');
            var normalizedFolder = NormalizeRelativeFolder(folder).Replace('\\', '/');

            return string.IsNullOrWhiteSpace(normalizedFolder)
                ? normalizedHostUrl
                : $"{normalizedHostUrl}/{normalizedFolder}";
        }

        private static string NormalizeUncRoot(string path)
        {
            return (path ?? string.Empty).Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string NormalizeRelativeFolder(string folder)
        {
            return (folder ?? string.Empty).Trim().Trim('/', '\\');
        }
    }
}
