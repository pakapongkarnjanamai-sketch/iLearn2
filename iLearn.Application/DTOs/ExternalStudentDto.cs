using System.ComponentModel.DataAnnotations;

namespace iLearn.Application.DTOs
{
    public class ExternalStudentDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Section { get; set; }
        public string Division { get; set; }
        public string Department { get; set; }
    }

    public class StudentDto
    {
        public int Id { get; set; }
        public string EId { get; set; }
        public string EnglishFirstName { get; set; }
        public string EnglishLastName { get; set; }
        public string Section { get; set; }
        public string Division { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }

    }
}