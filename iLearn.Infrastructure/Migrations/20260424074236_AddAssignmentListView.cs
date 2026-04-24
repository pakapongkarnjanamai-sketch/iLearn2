using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentListView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
        COUNT(DISTINCT ea.Id) AS StudentCount,
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
    COALESCE(ea.StudentCount,    0)           AS StudentCount,
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
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS [dbo].[vw_AssignmentList];");
        }
    }
}
