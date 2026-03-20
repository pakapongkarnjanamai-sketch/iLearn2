using System.Collections.Concurrent;

namespace iLearn.API.Services
{
    public interface IMaintenanceStatusService
    {
        Guid BeginOperation(string operationName, int totalItems, string initiatedBy);
        void UpdateOperation(Guid operationId, string currentStep, string? currentItemName = null, int? currentItem = null, int? successCount = null, int? failureCount = null);
        void CompleteOperation(Guid operationId, bool isSuccess, string completedStep, int successCount, int failureCount);
        IReadOnlyCollection<MaintenanceOperationStatus> GetActiveOperations();
    }

    public class MaintenanceStatusService : IMaintenanceStatusService
    {
        private readonly ConcurrentDictionary<Guid, MaintenanceOperationStatus> _operations = new();

        public Guid BeginOperation(string operationName, int totalItems, string initiatedBy)
        {
            var operationId = Guid.NewGuid();
            _operations[operationId] = new MaintenanceOperationStatus
            {
                OperationId = operationId,
                OperationName = operationName,
                TotalItems = totalItems,
                InitiatedBy = initiatedBy,
                StartedAt = DateTimeOffset.UtcNow,
                CurrentStep = "Starting"
            };

            return operationId;
        }

        public void UpdateOperation(Guid operationId, string currentStep, string? currentItemName = null, int? currentItem = null, int? successCount = null, int? failureCount = null)
        {
            if (!_operations.TryGetValue(operationId, out var operation))
                return;

            operation.CurrentStep = currentStep;
            operation.CurrentItemName = currentItemName ?? operation.CurrentItemName;
            operation.CurrentItem = currentItem ?? operation.CurrentItem;
            operation.SuccessCount = successCount ?? operation.SuccessCount;
            operation.FailureCount = failureCount ?? operation.FailureCount;
            operation.LastUpdatedAt = DateTimeOffset.UtcNow;
        }

        public void CompleteOperation(Guid operationId, bool isSuccess, string completedStep, int successCount, int failureCount)
        {
            if (!_operations.TryRemove(operationId, out var operation))
                return;

            operation.CurrentStep = completedStep;
            operation.SuccessCount = successCount;
            operation.FailureCount = failureCount;
            operation.CurrentItem = operation.TotalItems;
            operation.LastUpdatedAt = DateTimeOffset.UtcNow;
            operation.IsSuccess = isSuccess;
        }

        public IReadOnlyCollection<MaintenanceOperationStatus> GetActiveOperations()
        {
            return _operations.Values
                .OrderByDescending(x => x.StartedAt)
                .ToList();
        }
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
