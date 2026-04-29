using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameResourceStudentTerminology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS [dbo].[vw_AssignmentList];");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_StudentGroups_StudentGroupId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseResources_CourseVersions_CourseVersionId",
                table: "CourseResources");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseResources_Resources_ResourceId",
                table: "CourseResources");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_FileStorages_FileStorageId",
                table: "Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_ScormRuntimeStates_Resources_ResourceId",
                table: "ScormRuntimeStates");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentGroupCategories_Divisions_DivisionId",
                table: "StudentGroupCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentGroupCategories_StudentGroupCategories_ParentId",
                table: "StudentGroupCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentGroupMembers_StudentGroups_StudentGroupId",
                table: "StudentGroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentGroups_Divisions_DivisionId",
                table: "StudentGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentGroups_StudentGroupCategories_CategoryId",
                table: "StudentGroups");

            migrationBuilder.RenameTable(
                name: "Resources",
                newName: "ContentItems");

            migrationBuilder.RenameTable(
                name: "CourseResources",
                newName: "CourseContentItems");

            migrationBuilder.RenameTable(
                name: "StudentGroupCategories",
                newName: "LearnerGroupCategories");

            migrationBuilder.RenameTable(
                name: "StudentGroups",
                newName: "LearnerGroups");

            migrationBuilder.RenameTable(
                name: "StudentGroupMembers",
                newName: "LearnerGroupMembers");

            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_Resources]', N'PK_ContentItems', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_CourseResources]', N'PK_CourseContentItems', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_StudentGroupCategories]', N'PK_LearnerGroupCategories', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_StudentGroups]', N'PK_LearnerGroups', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_StudentGroupMembers]', N'PK_LearnerGroupMembers', N'OBJECT';");

            migrationBuilder.RenameColumn(
                name: "ResourceHref",
                table: "ContentItems",
                newName: "LaunchHref");

            migrationBuilder.RenameColumn(
                name: "ResourceId",
                table: "CourseContentItems",
                newName: "ContentItemId");

            migrationBuilder.RenameColumn(
                name: "StudentGroupId",
                table: "LearnerGroupMembers",
                newName: "LearnerGroupId");

            migrationBuilder.RenameColumn(
                name: "StudentCode",
                table: "LearnerGroupMembers",
                newName: "LearnerCode");

            migrationBuilder.RenameColumn(
                name: "ResourceId",
                table: "ScormRuntimeStates",
                newName: "ContentItemId");

            migrationBuilder.RenameColumn(
                name: "StudentCode",
                table: "LearningLogs",
                newName: "LearnerCode");

            migrationBuilder.RenameColumn(
                name: "ResourceId",
                table: "LearningLogs",
                newName: "ContentItemId");

            migrationBuilder.RenameColumn(
                name: "StudentCode",
                table: "Enrollments",
                newName: "LearnerCode");

            migrationBuilder.RenameColumn(
                name: "StudentGroupId",
                table: "Assignments",
                newName: "LearnerGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_Resources_FileStorageId",
                table: "ContentItems",
                newName: "IX_ContentItems_FileStorageId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseResources_CourseVersionId",
                table: "CourseContentItems",
                newName: "IX_CourseContentItems_CourseVersionId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseResources_ResourceId",
                table: "CourseContentItems",
                newName: "IX_CourseContentItems_ContentItemId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentGroupCategories_DivisionId",
                table: "LearnerGroupCategories",
                newName: "IX_LearnerGroupCategories_DivisionId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentGroupCategories_ParentId",
                table: "LearnerGroupCategories",
                newName: "IX_LearnerGroupCategories_ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentGroupCategories_Path",
                table: "LearnerGroupCategories",
                newName: "IX_LearnerGroupCategories_Path");

            migrationBuilder.RenameIndex(
                name: "IX_StudentGroups_CategoryId",
                table: "LearnerGroups",
                newName: "IX_LearnerGroups_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentGroups_DivisionId",
                table: "LearnerGroups",
                newName: "IX_LearnerGroups_DivisionId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentGroupMembers_StudentGroupId",
                table: "LearnerGroupMembers",
                newName: "IX_LearnerGroupMembers_LearnerGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_ScormRuntimeStates_ResourceId",
                table: "ScormRuntimeStates",
                newName: "IX_ScormRuntimeStates_ContentItemId");

            migrationBuilder.RenameIndex(
                name: "IX_ScormRuntimeStates_EnrollmentId_ResourceId",
                table: "ScormRuntimeStates",
                newName: "IX_ScormRuntimeStates_EnrollmentId_ContentItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignments_StudentGroupId",
                table: "Assignments",
                newName: "IX_Assignments_LearnerGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_LearnerGroups_LearnerGroupId",
                table: "Assignments",
                column: "LearnerGroupId",
                principalTable: "LearnerGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentItems_FileStorages_FileStorageId",
                table: "ContentItems",
                column: "FileStorageId",
                principalTable: "FileStorages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseContentItems_ContentItems_ContentItemId",
                table: "CourseContentItems",
                column: "ContentItemId",
                principalTable: "ContentItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseContentItems_CourseVersions_CourseVersionId",
                table: "CourseContentItems",
                column: "CourseVersionId",
                principalTable: "CourseVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LearnerGroupCategories_Divisions_DivisionId",
                table: "LearnerGroupCategories",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LearnerGroupCategories_LearnerGroupCategories_ParentId",
                table: "LearnerGroupCategories",
                column: "ParentId",
                principalTable: "LearnerGroupCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LearnerGroupMembers_LearnerGroups_LearnerGroupId",
                table: "LearnerGroupMembers",
                column: "LearnerGroupId",
                principalTable: "LearnerGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LearnerGroups_Divisions_DivisionId",
                table: "LearnerGroups",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LearnerGroups_LearnerGroupCategories_CategoryId",
                table: "LearnerGroups",
                column: "CategoryId",
                principalTable: "LearnerGroupCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScormRuntimeStates_ContentItems_ContentItemId",
                table: "ScormRuntimeStates",
                column: "ContentItemId",
                principalTable: "ContentItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(AssignmentListViewSql("LearnerCount"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS [dbo].[vw_AssignmentList];");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_LearnerGroups_LearnerGroupId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ContentItems_FileStorages_FileStorageId",
                table: "ContentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseContentItems_ContentItems_ContentItemId",
                table: "CourseContentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseContentItems_CourseVersions_CourseVersionId",
                table: "CourseContentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_LearnerGroupCategories_Divisions_DivisionId",
                table: "LearnerGroupCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_LearnerGroupCategories_LearnerGroupCategories_ParentId",
                table: "LearnerGroupCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_LearnerGroupMembers_LearnerGroups_LearnerGroupId",
                table: "LearnerGroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_LearnerGroups_Divisions_DivisionId",
                table: "LearnerGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_LearnerGroups_LearnerGroupCategories_CategoryId",
                table: "LearnerGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_ScormRuntimeStates_ContentItems_ContentItemId",
                table: "ScormRuntimeStates");

            migrationBuilder.RenameColumn(
                name: "LaunchHref",
                table: "ContentItems",
                newName: "ResourceHref");

            migrationBuilder.RenameColumn(
                name: "ContentItemId",
                table: "CourseContentItems",
                newName: "ResourceId");

            migrationBuilder.RenameColumn(
                name: "LearnerGroupId",
                table: "LearnerGroupMembers",
                newName: "StudentGroupId");

            migrationBuilder.RenameColumn(
                name: "LearnerCode",
                table: "LearnerGroupMembers",
                newName: "StudentCode");

            migrationBuilder.RenameColumn(
                name: "ContentItemId",
                table: "ScormRuntimeStates",
                newName: "ResourceId");

            migrationBuilder.RenameColumn(
                name: "LearnerCode",
                table: "LearningLogs",
                newName: "StudentCode");

            migrationBuilder.RenameColumn(
                name: "ContentItemId",
                table: "LearningLogs",
                newName: "ResourceId");

            migrationBuilder.RenameColumn(
                name: "LearnerCode",
                table: "Enrollments",
                newName: "StudentCode");

            migrationBuilder.RenameColumn(
                name: "LearnerGroupId",
                table: "Assignments",
                newName: "StudentGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_ContentItems_FileStorageId",
                table: "ContentItems",
                newName: "IX_Resources_FileStorageId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseContentItems_CourseVersionId",
                table: "CourseContentItems",
                newName: "IX_CourseResources_CourseVersionId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseContentItems_ContentItemId",
                table: "CourseContentItems",
                newName: "IX_CourseResources_ResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_LearnerGroupCategories_DivisionId",
                table: "LearnerGroupCategories",
                newName: "IX_StudentGroupCategories_DivisionId");

            migrationBuilder.RenameIndex(
                name: "IX_LearnerGroupCategories_ParentId",
                table: "LearnerGroupCategories",
                newName: "IX_StudentGroupCategories_ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_LearnerGroupCategories_Path",
                table: "LearnerGroupCategories",
                newName: "IX_StudentGroupCategories_Path");

            migrationBuilder.RenameIndex(
                name: "IX_LearnerGroups_CategoryId",
                table: "LearnerGroups",
                newName: "IX_StudentGroups_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_LearnerGroups_DivisionId",
                table: "LearnerGroups",
                newName: "IX_StudentGroups_DivisionId");

            migrationBuilder.RenameIndex(
                name: "IX_LearnerGroupMembers_LearnerGroupId",
                table: "LearnerGroupMembers",
                newName: "IX_StudentGroupMembers_StudentGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_ScormRuntimeStates_ContentItemId",
                table: "ScormRuntimeStates",
                newName: "IX_ScormRuntimeStates_ResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_ScormRuntimeStates_EnrollmentId_ContentItemId",
                table: "ScormRuntimeStates",
                newName: "IX_ScormRuntimeStates_EnrollmentId_ResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_Assignments_LearnerGroupId",
                table: "Assignments",
                newName: "IX_Assignments_StudentGroupId");

            migrationBuilder.RenameTable(
                name: "ContentItems",
                newName: "Resources");

            migrationBuilder.RenameTable(
                name: "CourseContentItems",
                newName: "CourseResources");

            migrationBuilder.RenameTable(
                name: "LearnerGroupCategories",
                newName: "StudentGroupCategories");

            migrationBuilder.RenameTable(
                name: "LearnerGroups",
                newName: "StudentGroups");

            migrationBuilder.RenameTable(
                name: "LearnerGroupMembers",
                newName: "StudentGroupMembers");

            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_ContentItems]', N'PK_Resources', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_CourseContentItems]', N'PK_CourseResources', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_LearnerGroupCategories]', N'PK_StudentGroupCategories', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_LearnerGroups]', N'PK_StudentGroups', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_LearnerGroupMembers]', N'PK_StudentGroupMembers', N'OBJECT';");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_StudentGroups_StudentGroupId",
                table: "Assignments",
                column: "StudentGroupId",
                principalTable: "StudentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseResources_CourseVersions_CourseVersionId",
                table: "CourseResources",
                column: "CourseVersionId",
                principalTable: "CourseVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseResources_Resources_ResourceId",
                table: "CourseResources",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_FileStorages_FileStorageId",
                table: "Resources",
                column: "FileStorageId",
                principalTable: "FileStorages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScormRuntimeStates_Resources_ResourceId",
                table: "ScormRuntimeStates",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentGroupCategories_Divisions_DivisionId",
                table: "StudentGroupCategories",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentGroupCategories_StudentGroupCategories_ParentId",
                table: "StudentGroupCategories",
                column: "ParentId",
                principalTable: "StudentGroupCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentGroupMembers_StudentGroups_StudentGroupId",
                table: "StudentGroupMembers",
                column: "StudentGroupId",
                principalTable: "StudentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentGroups_Divisions_DivisionId",
                table: "StudentGroups",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentGroups_StudentGroupCategories_CategoryId",
                table: "StudentGroups",
                column: "CategoryId",
                principalTable: "StudentGroupCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(AssignmentListViewSql("StudentCount"));
        }

        private static string AssignmentListViewSql(string enrollmentCountAlias) => $@"
CREATE VIEW [dbo].[vw_AssignmentList]
AS
WITH GroupedAssignments AS (
    SELECT
        COALESCE(NULLIF(LTRIM(RTRIM(a.AssignmentNo)), N''), N'assignment:' + CAST(a.Id AS NVARCHAR(20))) AS GroupKey,
        MIN(a.Id)                              AS Id,
        MIN(a.DivisionId)                      AS DivisionId,
        COALESCE(MIN(a.Description), N'')      AS Description,
        MIN(a.StartDate)                       AS StartDate,
        MIN(a.DueDate)                         AS DueDate,
        COALESCE(MIN(a.CreatedBy), N'')        AS CreatedBy,
        MIN(a.CreatedAt)                       AS CreatedAt
    FROM   dbo.Assignments a
    WHERE  a.IsDeleted = 0
    GROUP BY
        COALESCE(NULLIF(LTRIM(RTRIM(a.AssignmentNo)), N''), N'assignment:' + CAST(a.Id AS NVARCHAR(20)))
),
CourseAgg AS (
    SELECT
        ac.GroupKey,
        STRING_AGG(
            CASE WHEN c.IsDeleted = 0 THEN c.Title
                 ELSE c.Title + N' [Deleted]' END,
            N', '
        ) WITHIN GROUP (ORDER BY c.Title)              AS CourseNames,
        COUNT(DISTINCT c.Id)                           AS CourseCount,
        CAST(MAX(CASE WHEN c.IsDeleted = 1 THEN 1 ELSE 0 END) AS BIT) AS HasDeletedCourse
    FROM (
        SELECT DISTINCT
            COALESCE(NULLIF(LTRIM(RTRIM(a.AssignmentNo)), N''), N'assignment:' + CAST(a.Id AS NVARCHAR(20))) AS GroupKey,
            a.CourseId
        FROM   dbo.Assignments a
        WHERE  a.IsDeleted = 0
          AND  a.CourseId IS NOT NULL
    ) ac
    INNER JOIN dbo.Courses c ON c.Id = ac.CourseId
    GROUP BY ac.GroupKey
),
EnrollmentAgg AS (
    SELECT
        COALESCE(NULLIF(LTRIM(RTRIM(a.AssignmentNo)), N''), N'assignment:' + CAST(a.Id AS NVARCHAR(20))) AS GroupKey,
        COUNT(DISTINCT ea.Id) AS {enrollmentCountAlias},
        CAST(CASE
            WHEN COUNT(DISTINCT ea.Id) > 0
             AND COUNT(DISTINCT ea.Id) = SUM(CASE WHEN ea.SnapshotCompleted = 1 OR e.IsCompleted = 1 THEN 1 ELSE 0 END)
            THEN 1 ELSE 0
        END AS BIT) AS AllCompleted,
        CAST(CASE WHEN COUNT(DISTINCT ea.Id) > 0 THEN 1 ELSE 0 END AS BIT) AS HasEnrollments
    FROM   dbo.Assignments a
    LEFT JOIN dbo.EnrollmentAssignments ea ON ea.AssignmentId = a.Id AND ea.IsDeleted = 0
    LEFT JOIN dbo.Enrollments           e  ON e.Id = ea.EnrollmentId AND e.IsDeleted = 0
    WHERE  a.IsDeleted = 0
    GROUP BY
        COALESCE(NULLIF(LTRIM(RTRIM(a.AssignmentNo)), N''), N'assignment:' + CAST(a.Id AS NVARCHAR(20)))
)
SELECT
    g.Id,
    g.GroupKey                                           AS AssignmentNo,
    g.DivisionId,
    g.Description,
    g.StartDate,
    g.DueDate,
    g.CreatedBy,
    g.CreatedAt,
    COALESCE(c.CourseNames,      N'')         AS CourseNames,
    COALESCE(c.CourseCount,      0)           AS CourseCount,
    COALESCE(c.HasDeletedCourse, CAST(0 AS BIT)) AS HasDeletedCourse,
    COALESCE(ea.{enrollmentCountAlias}, 0)    AS {enrollmentCountAlias},
    COALESCE(ea.HasEnrollments,  CAST(0 AS BIT)) AS HasEnrollments,
    CASE
        WHEN COALESCE(ea.HasEnrollments, CAST(0 AS BIT)) = 1
         AND COALESCE(ea.AllCompleted,   CAST(0 AS BIT)) = 1 THEN N'Completed'
        WHEN g.StartDate IS NOT NULL AND g.StartDate > GETDATE()  THEN N'Upcoming'
        WHEN g.DueDate   IS NOT NULL AND g.DueDate   < GETDATE()  THEN N'Expired'
        ELSE N'InProgress'
    END AS Status
FROM      GroupedAssignments g
LEFT JOIN CourseAgg          c  ON c.GroupKey  = g.GroupKey
LEFT JOIN EnrollmentAgg      ea ON ea.GroupKey = g.GroupKey;
";
    }
}
