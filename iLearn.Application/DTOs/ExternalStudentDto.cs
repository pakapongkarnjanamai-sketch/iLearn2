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

    public class DivisionApiResponse
    {
        public List<DivisionStudentDto> data { get; set; }
        public int totalCount { get; set; }
        public int groupCount { get; set; }
        public List<int> summary { get; set; }
    }

    // คลาสสำหรับข้อมูลพนักงานแต่ละคนใน Array "data"
    public class DivisionStudentDto
    {
        public int Id { get; set; }
        public string EId { get; set; }
        public string EnglishFirstName { get; set; }
        public string EnglishLastName { get; set; }
        public string Section { get; set; }
        public string Division { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}