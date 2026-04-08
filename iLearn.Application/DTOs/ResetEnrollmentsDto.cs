using System.Collections.Generic;

namespace iLearn.Application.DTOs
{
    public class ResetEnrollmentsDto
    {
        /// <summary>
        /// Assignment rule IDs (each rule = one course in the batch).
        /// Empty or null means all courses.
        /// </summary>
        public List<int>? RuleIds { get; set; }

        /// <summary>
        /// Student codes to reset. Empty or null means all students.
        /// </summary>
        public List<string>? StudentCodes { get; set; }
    }
}
