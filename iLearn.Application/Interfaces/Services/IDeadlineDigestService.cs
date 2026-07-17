namespace iLearn.Application.Interfaces.Services
{
    public interface IDeadlineDigestService
    {
        Task<int> RunOnceAsync(CancellationToken ct = default);
    }
}