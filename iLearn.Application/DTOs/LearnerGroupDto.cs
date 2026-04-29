using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace iLearn.Application.DTOs
{
    public class LearnerGroupDto
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

    public class LearnerGroupDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CreatedBy { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        /// <summary>Ancestor category chain from root to direct parent (empty when group sits at root).</summary>
        public List<LearnerGroupCategoryAncestorDto> CategoryAncestors { get; set; } = new();
        public List<LearnerGroupMemberDto> Members { get; set; } = new();
    }

    public class LearnerGroupCategoryAncestorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class LearnerGroupMemberDto
    {
        public int Id { get; set; }
        public string LearnerCode { get; set; } = string.Empty;
        public string? LearnerName { get; set; }
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Position { get; set; }
    }

    public class CreateLearnerGroupDto
    {
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public List<string> LearnerCodes { get; set; } = new();
    }

    public class UpdateLearnerGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
    }

    public class AddGroupMembersDto
    {
        public List<string> LearnerCodes { get; set; } = new();
    }

    public class LearnerGroupAddMembersOptionsDto
    {
        public List<string> LearnerCodes { get; set; } = new();
        public bool EnrollToRelatedAssignments { get; set; }
        public List<string> AssignmentStatuses { get; set; } = new();
        public List<int> AssignmentIds { get; set; } = new();
    }

    public class LearnerGroupRemoveMembersOptionsDto
    {
        public List<int> MemberIds { get; set; } = new();
        public bool UnenrollFromRelatedAssignments { get; set; }
        public List<string> AssignmentStatuses { get; set; } = new();
        public List<int> AssignmentIds { get; set; } = new();
    }

    public class LearnerGroupRemoveMembersPreviewDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? GroupDescription { get; set; }
        public bool UnenrollFromRelatedAssignments { get; set; }
        public int SelectedMemberCount { get; set; }
        public int SelectedAssignmentCount { get; set; }
        public int SelectedCourseCount { get; set; }
        public int EstimatedUnenrollmentCount { get; set; }
        public List<LearnerGroupRemoveMembersLearnerPreviewDto> Members { get; set; } = new();
        public List<LearnerGroupRelatedAssignmentPreviewDto> Assignments { get; set; } = new();
    }

    public class LearnerGroupRemoveMembersLearnerPreviewDto
    {
        public int MemberId { get; set; }
        public string LearnerCode { get; set; } = string.Empty;
        public string LearnerName { get; set; } = string.Empty;
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Position { get; set; }
        public int CurrentAssignmentEnrollmentCount { get; set; }
    }

    public class LearnerGroupRemoveMembersResultDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int SelectedMemberCount { get; set; }
        public int RemovedMemberCount { get; set; }
        public int AssignmentCount { get; set; }
        public int UnenrolledLinkCount { get; set; }
        public List<string> RemovedLearnerCodes { get; set; } = new();
    }

    public class LearnerGroupAddMembersPreviewDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? GroupDescription { get; set; }
        public bool EnrollToRelatedAssignments { get; set; }
        public int SelectedLearnerCount { get; set; }
        public int NewMemberCount { get; set; }
        public int ExistingMemberCount { get; set; }
        public int SelectedAssignmentCount { get; set; }
        public int SelectedCourseCount { get; set; }
        public int EstimatedEnrollmentCount { get; set; }
        public List<LearnerGroupAddMembersLearnerPreviewDto> Learners { get; set; } = new();
        public List<LearnerGroupRelatedAssignmentPreviewDto> Assignments { get; set; } = new();
    }

    public class LearnerGroupAddMembersLearnerPreviewDto
    {
        public string LearnerCode { get; set; } = string.Empty;
        public string LearnerName { get; set; } = string.Empty;
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Position { get; set; }
        public bool IsAlreadyMember { get; set; }
    }

    public class LearnerGroupRelatedAssignmentPreviewDto
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

    public class LearnerGroupAddMembersResultDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int SelectedLearnerCount { get; set; }
        public int AddedMemberCount { get; set; }
        public int ExistingMemberCount { get; set; }
        public int AssignmentCount { get; set; }
        public int CourseCount { get; set; }
        public int EstimatedEnrollmentCount { get; set; }
        public List<string> AddedLearnerCodes { get; set; } = new();
    }
}
