using ClosedXML.Excel;
using iLearn.Application.DTOs;

namespace iLearn.Application.Services
{
    internal static class ReportExcelBuilder
    {
        private const string DateFormat = "dd/mm/yyyy";
        private const string PercentFormat = "0.0%";
        private const string GeneratedFormat = "dd/mm/yyyy hh:mm";

        public static byte[] BuildAssignmentWorkbook(AssignmentReportExportDto export, string? lang)
        {
            var labels = Labels.For(lang);
            using var workbook = new XLWorkbook();

            var summary = workbook.Worksheets.Add("Summary");
            WriteTitle(summary, labels.AssignmentReportTitle, export.From, export.To, export.GeneratedAt, labels, 14);
            WriteAssignmentSummary(summary, export.Summary.Rows, labels);

            var detail = workbook.Worksheets.Add("Detail");
            WriteTitle(detail, labels.AssignmentDetailTitle, export.From, export.To, export.GeneratedAt, labels, 11);
            WriteAssignmentDetail(detail, export.DetailRows, labels);

            return Save(workbook);
        }

        public static byte[] BuildLearnerGroupWorkbook(LearnerGroupReportExportDto export, string? lang)
        {
            var labels = Labels.For(lang);
            using var workbook = new XLWorkbook();

            var summary = workbook.Worksheets.Add("Summary");
            WriteTitle(summary, labels.LearnerGroupReportTitle, export.From, export.To, export.GeneratedAt, labels, 14);
            WriteLearnerGroupSummary(summary, export.Summary.Rows, labels);

            var members = workbook.Worksheets.Add("Members");
            WriteTitle(members, labels.LearnerGroupMembersTitle, export.From, export.To, export.GeneratedAt, labels, 5);
            WriteLearnerGroupMembers(members, export.MemberRows, labels);

            var detail = workbook.Worksheets.Add("Detail");
            WriteTitle(detail, labels.LearnerGroupDetailTitle, export.From, export.To, export.GeneratedAt, labels, 11);
            WriteLearnerGroupDetail(detail, export.DetailRows, labels);

            return Save(workbook);
        }

        private static void WriteTitle(IXLWorksheet worksheet, string title, DateTime? from, DateTime? to, DateTime generatedAt, Labels labels, int columns)
        {
            var titleRange = worksheet.Range(1, 1, 1, columns);
            titleRange.Merge();
            titleRange.Value = title;
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 14;

            var rangeText = FormatRange(from, to, labels);
            var rangeCell = worksheet.Cell(2, 1);
            rangeCell.Value = $"{labels.DateRange}: {rangeText}";
            worksheet.Range(2, 1, 2, columns).Merge();

            var generatedCell = worksheet.Cell(3, 1);
            generatedCell.Value = $"{labels.GeneratedAt}:";
            generatedCell.Style.Font.Bold = true;
            var generatedValue = worksheet.Cell(3, 2);
            generatedValue.Value = generatedAt;
            generatedValue.Style.DateFormat.Format = GeneratedFormat;
        }

        private static void WriteAssignmentSummary(IXLWorksheet worksheet, IReadOnlyList<AssignmentSummaryRow> rows, Labels labels)
        {
            var headerRow = 5;
            var headers = new[]
            {
                labels.AssignmentNo, labels.Description, labels.Division, labels.StatusHeader, labels.CreatedAt,
                labels.StartDate, labels.DueDate, labels.Courses, labels.Learners, labels.Enrollments,
                labels.Completed, labels.Overdue, labels.CompletionRate
            };
            WriteHeader(worksheet, headerRow, headers);

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var excelRow = headerRow + 1 + index;
                worksheet.Cell(excelRow, 1).Value = row.AssignmentNo;
                worksheet.Cell(excelRow, 2).Value = row.Description ?? string.Empty;
                worksheet.Cell(excelRow, 3).Value = row.DivisionName ?? string.Empty;
                worksheet.Cell(excelRow, 4).Value = labels.Status(row.Status);
                SetDate(worksheet.Cell(excelRow, 5), row.CreatedAt);
                SetDate(worksheet.Cell(excelRow, 6), row.StartDate);
                SetDate(worksheet.Cell(excelRow, 7), row.DueDate);
                worksheet.Cell(excelRow, 8).Value = row.CourseCount;
                worksheet.Cell(excelRow, 9).Value = row.LearnerCount;
                worksheet.Cell(excelRow, 10).Value = row.EnrollmentCount;
                worksheet.Cell(excelRow, 11).Value = row.CompletedCount;
                worksheet.Cell(excelRow, 12).Value = row.OverdueCount;
                SetPercent(worksheet.Cell(excelRow, 13), row.CompletionRate);
            }

            FinalizeSheet(worksheet, headerRow, rows.Count, headers.Length, [18, 28, 18, 16, 13, 13, 13, 10, 10, 12, 12, 12, 14]);
        }

        private static void WriteAssignmentDetail(IXLWorksheet worksheet, IReadOnlyList<AssignmentReportDetailRow> rows, Labels labels)
        {
            var headerRow = 5;
            var headers = new[]
            {
                labels.AssignmentNo, labels.LearnerCode, labels.LearnerName, labels.LearnerDivision,
                labels.CourseTitle, labels.StartDate, labels.DueDate, labels.StatusHeader, labels.Progress,
                labels.CompletedDate, labels.DaysOverdue
            };
            WriteHeader(worksheet, headerRow, headers);

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var excelRow = headerRow + 1 + index;
                worksheet.Cell(excelRow, 1).Value = row.AssignmentNo;
                worksheet.Cell(excelRow, 2).Value = row.LearnerCode;
                worksheet.Cell(excelRow, 3).Value = row.LearnerName ?? string.Empty;
                worksheet.Cell(excelRow, 4).Value = row.LearnerDivision ?? string.Empty;
                worksheet.Cell(excelRow, 5).Value = BuildCourseText(row.CourseCode, row.CourseTitle);
                SetDate(worksheet.Cell(excelRow, 6), row.StartDate);
                SetDate(worksheet.Cell(excelRow, 7), row.DueDate);
                worksheet.Cell(excelRow, 8).Value = labels.Status(row.Status);
                SetPercent(worksheet.Cell(excelRow, 9), row.Progress);
                SetDate(worksheet.Cell(excelRow, 10), row.CompletedDate);
                worksheet.Cell(excelRow, 11).Value = row.DaysOverdue;
            }

            FinalizeSheet(worksheet, headerRow, rows.Count, headers.Length, [18, 14, 24, 18, 34, 13, 13, 16, 12, 13, 12]);
        }

        private static void WriteLearnerGroupSummary(IXLWorksheet worksheet, IReadOnlyList<LearnerGroupSummaryRow> rows, Labels labels)
        {
            var headerRow = 5;
            var headers = new[]
            {
                labels.GroupName, labels.Description, labels.Division, labels.Category, labels.CreatedAt,
                labels.DueDate, labels.Members, labels.Assignments, labels.Courses, labels.Enrollments,
                labels.Completed, labels.Overdue, labels.AvgProgress, labels.CompletionRate
            };
            WriteHeader(worksheet, headerRow, headers);

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var excelRow = headerRow + 1 + index;
                worksheet.Cell(excelRow, 1).Value = row.Name;
                worksheet.Cell(excelRow, 2).Value = row.Description ?? string.Empty;
                worksheet.Cell(excelRow, 3).Value = row.DivisionName ?? string.Empty;
                worksheet.Cell(excelRow, 4).Value = row.CategoryName ?? string.Empty;
                SetDate(worksheet.Cell(excelRow, 5), row.CreatedAt);
                SetDate(worksheet.Cell(excelRow, 6), row.DueDate);
                worksheet.Cell(excelRow, 7).Value = row.MemberCount;
                worksheet.Cell(excelRow, 8).Value = row.AssignmentCount;
                worksheet.Cell(excelRow, 9).Value = row.CourseCount;
                worksheet.Cell(excelRow, 10).Value = row.EnrollmentCount;
                worksheet.Cell(excelRow, 11).Value = row.CompletedCount;
                worksheet.Cell(excelRow, 12).Value = row.OverdueCount;
                SetPercent(worksheet.Cell(excelRow, 13), row.AvgProgress);
                SetPercent(worksheet.Cell(excelRow, 14), row.CompletionRate);
            }

            FinalizeSheet(worksheet, headerRow, rows.Count, headers.Length, [24, 28, 18, 18, 13, 13, 10, 12, 10, 12, 12, 12, 14, 14]);
        }

        private static void WriteLearnerGroupMembers(IXLWorksheet worksheet, IReadOnlyList<LearnerGroupReportMemberRow> rows, Labels labels)
        {
            var headerRow = 5;
            var headers = new[] { labels.GroupName, labels.LearnerCode, labels.LearnerName, labels.LearnerDivision, labels.MemberSince };
            WriteHeader(worksheet, headerRow, headers);

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var excelRow = headerRow + 1 + index;
                worksheet.Cell(excelRow, 1).Value = row.GroupName;
                worksheet.Cell(excelRow, 2).Value = row.LearnerCode;
                worksheet.Cell(excelRow, 3).Value = row.LearnerName ?? string.Empty;
                worksheet.Cell(excelRow, 4).Value = row.LearnerDivision ?? string.Empty;
                SetDate(worksheet.Cell(excelRow, 5), row.CreatedAt);
            }

            FinalizeSheet(worksheet, headerRow, rows.Count, headers.Length, [24, 14, 24, 18, 13]);
        }

        private static void WriteLearnerGroupDetail(IXLWorksheet worksheet, IReadOnlyList<LearnerGroupReportDetailRow> rows, Labels labels)
        {
            var headerRow = 5;
            var headers = new[]
            {
                labels.GroupName, labels.LearnerCode, labels.LearnerName, labels.CourseTitle,
                labels.AssignmentNo, labels.StartDate, labels.DueDate, labels.StatusHeader, labels.Progress,
                labels.CompletedDate, labels.DaysOverdue
            };
            WriteHeader(worksheet, headerRow, headers);

            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var excelRow = headerRow + 1 + index;
                worksheet.Cell(excelRow, 1).Value = row.GroupName;
                worksheet.Cell(excelRow, 2).Value = row.LearnerCode;
                worksheet.Cell(excelRow, 3).Value = row.LearnerName ?? string.Empty;
                worksheet.Cell(excelRow, 4).Value = BuildCourseText(row.CourseCode, row.CourseTitle);
                worksheet.Cell(excelRow, 5).Value = row.AssignmentNo ?? string.Empty;
                SetDate(worksheet.Cell(excelRow, 6), row.StartDate);
                SetDate(worksheet.Cell(excelRow, 7), row.DueDate);
                worksheet.Cell(excelRow, 8).Value = labels.Status(row.Status);
                SetPercent(worksheet.Cell(excelRow, 9), row.Progress);
                SetDate(worksheet.Cell(excelRow, 10), row.CompletedDate);
                worksheet.Cell(excelRow, 11).Value = row.DaysOverdue;
            }

            FinalizeSheet(worksheet, headerRow, rows.Count, headers.Length, [24, 14, 24, 34, 18, 13, 13, 16, 12, 13, 12]);
        }

        private static void WriteHeader(IXLWorksheet worksheet, int rowNumber, IReadOnlyList<string> headers)
        {
            for (var index = 0; index < headers.Count; index++)
            {
                var cell = worksheet.Cell(rowNumber, index + 1);
                cell.Value = headers[index];
            }

            var range = worksheet.Range(rowNumber, 1, rowNumber, headers.Count);
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("EAF1FB");
            range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static void FinalizeSheet(IXLWorksheet worksheet, int headerRow, int dataRowCount, int columnCount, IReadOnlyList<double> widths)
        {
            worksheet.SheetView.FreezeRows(headerRow);
            worksheet.Range(headerRow, 1, Math.Max(headerRow + dataRowCount, headerRow), columnCount).SetAutoFilter();

            for (var index = 0; index < columnCount; index++)
            {
                worksheet.Column(index + 1).Width = index < widths.Count ? widths[index] : 16;
            }

            worksheet.Rows(1, Math.Max(headerRow + dataRowCount, headerRow)).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        }

        private static void SetDate(IXLCell cell, DateTime? value)
        {
            if (!value.HasValue)
            {
                cell.Value = string.Empty;
                return;
            }

            cell.Value = value.Value;
            cell.Style.DateFormat.Format = DateFormat;
        }

        private static void SetPercent(IXLCell cell, double percentValue)
        {
            cell.Value = percentValue / 100.0;
            cell.Style.NumberFormat.Format = PercentFormat;
        }

        private static string BuildCourseText(string? code, string? title)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return title ?? string.Empty;
            }

            return string.IsNullOrWhiteSpace(title) ? code : $"{code} - {title}";
        }

        private static string FormatRange(DateTime? from, DateTime? to, Labels labels)
        {
            if (!from.HasValue && !to.HasValue)
            {
                return labels.AllDates;
            }

            return $"{FormatDate(from, labels)} - {FormatDate(to, labels)}";
        }

        private static string FormatDate(DateTime? value, Labels labels)
        {
            return value.HasValue ? value.Value.ToString("dd/MM/yyyy") : labels.OpenEnded;
        }

        private static byte[] Save(XLWorkbook workbook)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private sealed class Labels
        {
            private readonly bool _isEnglish;

            private Labels(bool isEnglish)
            {
                _isEnglish = isEnglish;
            }

            public static Labels For(string? lang)
            {
                return new Labels(string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase));
            }

            public string AssignmentReportTitle => Text("รายงานสรุปงานมอบหมาย", "Assignment Summary Report");
            public string AssignmentDetailTitle => Text("รายละเอียดรายคน", "Detail");
            public string LearnerGroupReportTitle => Text("รายงานสรุปกลุ่มผู้เรียน", "Learner Group Summary Report");
            public string LearnerGroupMembersTitle => Text("สมาชิก", "Members");
            public string LearnerGroupDetailTitle => Text("รายละเอียดรายคน", "Detail");
            public string DateRange => Text("ช่วงวันที่", "Date range");
            public string GeneratedAt => Text("สร้างเมื่อ", "Generated at");
            public string AllDates => Text("ทั้งหมด", "All dates");
            public string OpenEnded => Text("ไม่ระบุ", "Open-ended");
            public string AssignmentNo => Text("เลขที่งานมอบหมาย", "Assignment No.");
            public string Description => Text("คำอธิบาย", "Description");
            public string Division => Text("สายงาน", "Division");
            public string StatusHeader => Text("สถานะ", "Status");
            public string CreatedAt => Text("วันที่สร้าง", "Created");
            public string StartDate => Text("วันเริ่ม", "Start Date");
            public string DueDate => Text("วันครบกำหนด", "Due Date");
            public string Courses => Text("คอร์ส", "Courses");
            public string Learners => Text("ผู้เรียน", "Learners");
            public string Enrollments => Text("รายการเรียน", "Enrollments");
            public string Completed => Text("สำเร็จ", "Completed");
            public string Overdue => Text("เกินกำหนด", "Overdue");
            public string CompletionRate => Text("อัตราการเรียนสำเร็จ", "Completion rate");
            public string LearnerCode => Text("รหัสพนักงาน", "Learner Code");
            public string LearnerName => Text("ชื่อ-สกุล", "Learner Name");
            public string LearnerDivision => Text("สายงานผู้เรียน", "Learner Division");
            public string CourseTitle => Text("คอร์ส", "Course");
            public string Progress => Text("ความคืบหน้า", "Progress");
            public string CompletedDate => Text("วันที่เรียนจบ", "Completed Date");
            public string DaysOverdue => Text("เกินกำหนด (วัน)", "Days Overdue");
            public string GroupName => Text("ชื่อกลุ่ม", "Group Name");
            public string Category => Text("หมวดหมู่", "Category");
            public string Members => Text("สมาชิก", "Members");
            public string Assignments => Text("งานมอบหมาย", "Assignments");
            public string AvgProgress => Text("ความคืบหน้าเฉลี่ย", "Avg. Progress");
            public string MemberSince => Text("วันที่เข้ากลุ่ม", "Member Since");

            public string Status(string status) => status switch
            {
                "Completed" => Text("สำเร็จ", "Completed"),
                "Upcoming" => Text("กำลังจะถึง", "Upcoming"),
                "Expired" => Text("เกินกำหนด", "Expired"),
                "Overdue" => Text("เกินกำหนด", "Overdue"),
                "InProgress" => Text("กำลังดำเนินการ", "In Progress"),
                "NotStarted" => Text("ยังไม่เริ่ม", "Not Started"),
                _ => status,
            };

            private string Text(string th, string en) => _isEnglish ? en : th;
        }
    }
}