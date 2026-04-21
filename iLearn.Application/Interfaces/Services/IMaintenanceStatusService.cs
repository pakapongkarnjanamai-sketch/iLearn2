namespace iLearn.Application.Interfaces.Services
{
    /// <summary>
    /// Tracks the progress of long-running maintenance operations (bulk imports,
    /// SCORM rebuilds, etc.) so the Admin UI can poll status without holding an
    /// HTTP connection open. State is in-process and best-effort; it is not
    /// persisted across restarts.
    /// </summary>
    public interface IMaintenanceStatusService
    {
        Guid BeginOperation(string operationName, int totalItems, string initiatedBy);

        void UpdateOperation(
            Guid operationId,
            string currentStep,
            string? currentItemName = null,
            int? currentItem = null,
            int? successCount = null,
            int? failureCount = null);

        void CompleteOperation(
            Guid operationId,
            bool isSuccess,
            string completedStep,
            int successCount,
            int failureCount);

        IReadOnlyCollection<MaintenanceOperationStatus> GetActiveOperations();
    }

    public class MaintenanceOperationStatus
    {
        public Guid OperationId { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public string CurrentStep { get; set; } = string.Empty;
        public string? CurrentItemName { get; set; }
        public int CurrentItem { get; set; }
        public int TotalItems { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public string InitiatedBy { get; set; } = string.Empty;
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? LastUpdatedAt { get; set; }
        public bool IsSuccess { get; set; }
    }
}
