using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    public interface IReportService
    {
        Task<ComplianceReportDto> GetComplianceReportAsync(int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default);
        Task<TranscriptReportDto> GetTranscriptReportAsync(string learnerCode, int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default);
        Task<CourseSummaryReportDto> GetCourseSummaryReportAsync(int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default);
        Task<AssignmentSummaryReportDto> GetAssignmentSummaryReportAsync(int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default);
        Task<LearnerGroupSummaryReportDto> GetLearnerGroupSummaryReportAsync(int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default);
        Task<AssignmentReportExportDto> GetAssignmentReportExportAsync(int? divisionId, DateTime? from, DateTime? to, DateTime currentDate, CancellationToken cancellationToken = default);
        Task<LearnerGroupReportExportDto> GetLearnerGroupReportExportAsync(int? divisionId, DateTime? from, DateTime? to, DateTime currentDate, CancellationToken cancellationToken = default);
        Task<byte[]> BuildAssignmentReportExcelAsync(int? divisionId, DateTime? from, DateTime? to, string? lang, DateTime currentDate, CancellationToken cancellationToken = default);
        Task<byte[]> BuildLearnerGroupReportExcelAsync(int? divisionId, DateTime? from, DateTime? to, string? lang, DateTime currentDate, CancellationToken cancellationToken = default);
        Task<ActivityReportDto> GetActivityReportAsync(int months, int? divisionId, CancellationToken cancellationToken = default);
    }
}
