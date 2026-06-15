using System.Collections.Generic;
using iLearn.Application.Interfaces.Services;

namespace iLearn.Application.DTOs
{
    public class BulkAssignResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; } // "Forbid", "Conflict", "BadRequest"
        public string? AssignmentNo { get; set; }
        public int AssignmentId { get; set; }
        public List<ConflictDto> InProgressConflicts { get; set; } = [];
        public List<CompletedConflictDto> CompletedConflicts { get; set; } = [];
    }
}
