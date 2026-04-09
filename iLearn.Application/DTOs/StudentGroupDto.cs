using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
        public string? CreatedBy { get; set; }
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

    public class StudentGroupAddMembersOptionsDto
    {
        public List<string> StudentCodes { get; set; } = new();
        public bool EnrollToRelatedAssignments { get; set; }
        public List<string> AssignmentStatuses { get; set; } = new();
    }

    public class StudentGroupAddMembersPreviewDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? GroupDescription { get; set; }
        public bool EnrollToRelatedAssignments { get; set; }
        public int SelectedStudentCount { get; set; }
        public int NewMemberCount { get; set; }
        public int ExistingMemberCount { get; set; }
        public int SelectedAssignmentCount { get; set; }
        public int SelectedCourseCount { get; set; }
        public int EstimatedEnrollmentCount { get; set; }
        public List<StudentGroupAddMembersStudentPreviewDto> Students { get; set; } = new();
        public List<StudentGroupRelatedAssignmentPreviewDto> Assignments { get; set; } = new();
    }

    public class StudentGroupAddMembersStudentPreviewDto
    {
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Position { get; set; }
        public bool IsAlreadyMember { get; set; }
    }

    public class StudentGroupRelatedAssignmentPreviewDto
    {
        public int Id { get; set; }
        public string? AssignmentNo { get; set; }
        public string? Description { get; set; }
        public string CourseNames { get; set; } = string.Empty;
        public int CourseCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int CurrentLearnerCount { get; set; }
        public int EstimatedEnrollmentCount { get; set; }
    }

    public class StudentGroupAddMembersResultDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int SelectedStudentCount { get; set; }
        public int AddedMemberCount { get; set; }
        public int ExistingMemberCount { get; set; }
        public int AssignmentCount { get; set; }
        public int CourseCount { get; set; }
        public int EstimatedEnrollmentCount { get; set; }
        public List<string> AddedStudentCodes { get; set; } = new();
    }
}
