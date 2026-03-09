
namespace iLearn.Application.Interfaces
{
    /// <summary>
    /// Unit of Work pattern — ใช้ควบคุม transaction ให้ SaveChanges ทีเดียว
    /// แทนที่จะ SaveChanges ทุกครั้งที่เรียก Repository method
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
