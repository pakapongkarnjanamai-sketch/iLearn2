using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    public interface IReportService
    {
        Task<ComplianceReportDto> GetComplianceReportAsync(int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default);
        Task<TranscriptReportDto> GetTranscriptReportAsync(string learnerCode, int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default);
        Task<CourseSummaryReportDto> GetCourseSummaryReportAsync(int? divisionId, DateTime currentDate, CancellationToken cancellationToken = default);
        Task<ActivityReportDto> GetActivityReportAsync(int months, int? divisionId, CancellationToken cancellationToken = default);
    }
}
