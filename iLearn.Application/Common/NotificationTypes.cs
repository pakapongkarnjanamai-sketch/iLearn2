namespace iLearn.Application.Common
{
    public static class NotificationTypes
    {
        public const string ScormUploadSucceeded = "ScormUploadSucceeded";
        public const string ScormUploadFailed = "ScormUploadFailed";
        public const string ContentPublishSucceeded = "ContentPublishSucceeded";
        public const string ContentPublishFailed = "ContentPublishFailed";
        public const string BatchPublishCompleted = "BatchPublishCompleted";
        public const string BulkAssignCompleted = "BulkAssignCompleted";
    }

    public static class NotificationLevels
    {
        public const string Success = "success";
        public const string Error = "error";
        public const string Info = "info";
    }
}
