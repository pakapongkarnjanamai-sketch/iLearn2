namespace iLearn.Application.Common
{
    public static class ScormContentStatusPolicy
    {
        public const int LearnTypeId = 1;
        public const int ExamTypeId = 2;

        public static string ResolveStatus(
            int? contentTypeId,
            string? lessonStatus,
            string? completionStatus,
            string? successStatus,
            string? persistedStatus = null,
            bool isDone = false)
        {
            var normalizedLessonStatus = NormalizeStatus(lessonStatus);
            var normalizedCompletionStatus = NormalizeStatus(completionStatus);
            var normalizedSuccessStatus = NormalizeStatus(successStatus);
            var normalizedPersistedStatus = NormalizeStatus(persistedStatus);

            if (normalizedSuccessStatus == "failed" ||
                normalizedLessonStatus == "failed" ||
                normalizedPersistedStatus == "failed")
            {
                return "failed";
            }

            if (normalizedSuccessStatus == "passed" ||
                normalizedLessonStatus == "passed" ||
                normalizedPersistedStatus == "passed")
            {
                return "passed";
            }

            if (normalizedCompletionStatus == "completed" ||
                normalizedLessonStatus == "completed" ||
                normalizedLessonStatus == "browsed" ||
                normalizedPersistedStatus == "completed" ||
                isDone)
            {
                return IsExamType(contentTypeId) ? "incomplete" : "completed";
            }

            return "incomplete";
        }

        public static double ResolveCompletionProgress(string? status)
        {
            return NormalizeStatus(status) is "passed" or "completed" ? 100 : 0;
        }

        public static bool IsExamType(int? contentTypeId)
        {
            return contentTypeId == ExamTypeId;
        }

        private static string? NormalizeStatus(string? status)
        {
            return string.IsNullOrWhiteSpace(status)
                ? null
                : status.Trim().ToLowerInvariant();
        }
    }
}