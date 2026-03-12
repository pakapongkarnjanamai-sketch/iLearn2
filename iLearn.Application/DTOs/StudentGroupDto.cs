using System;
using System.Collections.Generic;

namespace iLearn.Application.DTOs
{
    public class StudentGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MemberCount { get; set; }
        public int? DivisionId { get; set; }          // 🆕 เพิ่ม
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class StudentGroupDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<StudentGroupMemberDto> Members { get; set; } = new();
    }

    public class StudentGroupMemberDto
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Position { get; set; }
    }

    public class CreateStudentGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> StudentCodes { get; set; } = new();
    }

    public class UpdateStudentGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class AddGroupMembersDto
    {
        public List<string> StudentCodes { get; set; } = new();
    }
}
