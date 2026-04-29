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
        /// Learner codes to reset. Empty or null means all learners.
        /// </summary>
        public List<string>? LearnerCodes { get; set; }
    }
}
