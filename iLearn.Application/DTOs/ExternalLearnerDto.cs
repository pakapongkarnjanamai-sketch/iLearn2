namespace iLearn.Application.DTOs
{
    public class AllLearnersApiResponse
    {
        public bool success { get; set; }
        public List<LearnerDto> data { get; set; } // ใช้ List<LearnerDto> เพื่อรับ Array ของพนักงาน
    }
    public class ExternalLearnerDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Section { get; set; }
        public string Division { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
    }

    public class LearnerDto
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

    public class EmployeeCsvApiResponse
    {
        public List<EmployeeCsvDto> data { get; set; } = new();
        public int totalCount { get; set; }
        public int groupCount { get; set; }
    }

    public class EmployeeCsvDto
    {
        public int Id { get; set; }
        public string EId { get; set; } = string.Empty;
        public string NID { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ThaiFirstName { get; set; } = string.Empty;
        public string ThaiLastName { get; set; } = string.Empty;
        public string EnglishFirstName { get; set; } = string.Empty;
        public string EnglishLastName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;

        public string FullName
        {
            get
            {
                var englishName = $"{EnglishFirstName} {EnglishLastName}".Trim();
                if (!string.IsNullOrWhiteSpace(englishName))
                    return englishName;

                return $"{ThaiFirstName} {ThaiLastName}".Trim();
            }
        }
    }

    public class DivisionApiResponse
    {
        public List<DivisionLearnerDto> data { get; set; }
        public int totalCount { get; set; }
        public int groupCount { get; set; }
        public List<int> summary { get; set; }
    }

    // คลาสสำหรับข้อมูลพนักงานแต่ละคนใน Array "data"
    public class DivisionLearnerDto
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