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
        public int? DivisionId { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class StudentGroupDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CreatedBy { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        /// <summary>Ancestor category chain from root to direct parent (empty when group sits at root).</summary>
        public List<StudentGroupCategoryAncestorDto> CategoryAncestors { get; set; } = new();
        public List<StudentGroupMemberDto> Members { get; set; } = new();
    }

    public class StudentGroupCategoryAncestorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
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
        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public List<string> StudentCodes { get; set; } = new();
    }

    public class UpdateStudentGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
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
        public List<int> AssignmentIds { get; set; } = new();
    }

    public class StudentGroupRemoveMembersOptionsDto
    {
        public List<int> MemberIds { get; set; } = new();
        public bool UnenrollFromRelatedAssignments { get; set; }
        public List<string> AssignmentStatuses { get; set; } = new();
        public List<int> AssignmentIds { get; set; } = new();
    }

    public class StudentGroupRemoveMembersPreviewDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? GroupDescription { get; set; }
        public bool UnenrollFromRelatedAssignments { get; set; }
        public int SelectedMemberCount { get; set; }
        public int SelectedAssignmentCount { get; set; }
        public int SelectedCourseCount { get; set; }
        public int EstimatedUnenrollmentCount { get; set; }
        public List<StudentGroupRemoveMembersStudentPreviewDto> Members { get; set; } = new();
        public List<StudentGroupRelatedAssignmentPreviewDto> Assignments { get; set; } = new();
    }

    public class StudentGroupRemoveMembersStudentPreviewDto
    {
        public int MemberId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Position { get; set; }
        public int CurrentAssignmentEnrollmentCount { get; set; }
    }

    public class StudentGroupRemoveMembersResultDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int SelectedMemberCount { get; set; }
        public int RemovedMemberCount { get; set; }
        public int AssignmentCount { get; set; }
        public int UnenrolledLinkCount { get; set; }
        public List<string> RemovedStudentCodes { get; set; } = new();
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
