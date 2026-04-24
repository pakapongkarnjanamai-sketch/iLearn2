namespace iLearn.Application.Interfaces.Services
{
    /// <summary>
    /// Generates unique AssignmentNo values in the format AS-yyyyMMdd-NNN,
    /// where the running number resets for each date prefix.
    /// </summary>
    public interface IAssignmentNoGenerator
    {
        /// <summary>
        /// Returns the next AssignmentNo in the format AS-yyyyMMdd-NNN.
        /// The running number is calculated per date prefix.
        /// </summary>
        Task<string> NextAsync();
    }
}
