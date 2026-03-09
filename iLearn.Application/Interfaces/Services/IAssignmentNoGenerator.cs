namespace iLearn.Application.Interfaces.Services
{
    /// <summary>
    /// Generates unique AssignmentNo values using a database sequence to prevent race conditions.
    /// </summary>
    public interface IAssignmentNoGenerator
    {
        /// <summary>
        /// Returns the next AssignmentNo in the format AS-yyyyMMdd-NNN.
        /// The running number is obtained from a DB sequence (atomic, no race condition).
        /// </summary>
        Task<string> NextAsync();
    }
}
