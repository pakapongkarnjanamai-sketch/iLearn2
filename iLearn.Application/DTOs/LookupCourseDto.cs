using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLearn.Application.DTOs
{
    public class LookupCourseDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public int? CategoryId { get; set; }
        public int? DivisionId { get; set; }
        public int? CourseTypeId { get; set; }
        public string? CourseTypeName { get; set; }
    }

    public class LookupLearnerDto
    {
        public string EmpCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Division { get; set; }
        public string Department { get; set; }
    }
}
