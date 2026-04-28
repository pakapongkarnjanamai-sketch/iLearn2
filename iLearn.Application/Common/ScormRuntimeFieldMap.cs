namespace iLearn.Application.Common
{
    public static class ScormRuntimeFieldMap
    {
        public const string Scorm12 = "1.2";
        public const string Scorm2004 = "2004";

        public static readonly IReadOnlyDictionary<string, string[]> CanonicalAliases =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["lesson_location"] = ["cmi.core.lesson_location", "cmi.location"],
                ["suspend_data"] = ["cmi.suspend_data"],
                ["lesson_status"] = ["cmi.core.lesson_status"],
                ["completion_status"] = ["cmi.completion_status"],
                ["success_status"] = ["cmi.success_status"],
                ["raw_score"] = ["cmi.core.score.raw", "cmi.score.raw"],
                ["session_time"] = ["cmi.core.session_time", "cmi.session_time"],
                ["total_time"] = ["cmi.core.total_time", "cmi.total_time"],
                ["entry"] = ["cmi.core.entry", "cmi.entry"],
                ["exit"] = ["cmi.core.exit", "cmi.exit"]
            };

        public static string NormalizeVersion(string? scormVersion)
        {
            if (string.IsNullOrWhiteSpace(scormVersion))
            {
                return string.Empty;
            }

            if (scormVersion.Contains("2004", StringComparison.OrdinalIgnoreCase))
            {
                return Scorm2004;
            }

            if (scormVersion.Contains("1.2", StringComparison.OrdinalIgnoreCase))
            {
                return Scorm12;
            }

            return scormVersion.Trim();
        }

        public static string? NormalizeCompletionStatus(string? lessonStatus, string? completionStatus)
        {
            if (!string.IsNullOrWhiteSpace(completionStatus))
            {
                return completionStatus;
            }

            return lessonStatus?.ToLowerInvariant() switch
            {
                "completed" => "completed",
                "passed" => "completed",
                "failed" => "completed",
                "incomplete" => "incomplete",
                "browsed" => "completed",
                "not attempted" => "not attempted",
                _ => lessonStatus
            };
        }

        public static string? NormalizeSuccessStatus(string? lessonStatus, string? successStatus)
        {
            if (!string.IsNullOrWhiteSpace(successStatus))
            {
                return successStatus;
            }

            return lessonStatus?.ToLowerInvariant() switch
            {
                "passed" => "passed",
                "failed" => "failed",
                _ => null
            };
        }
    }
}