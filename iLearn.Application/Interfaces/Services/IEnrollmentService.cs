using iLearn.Application.DTOs;
using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface IEnrollmentService
    {
        Task<EnrollmentDto?> ResetStatusAsync(int enrollmentId);
        Task<EnrollmentDto?> GetByIdAsync(int enrollmentId);
        Task<EnrollmentDto?> UpdateCompletionAsync(int enrollmentId, bool isComplete);
        Task<BulkAssignResultDto> BulkAssignAsync(BulkAssignDto dto);
    }
}
