using ArcherComparisonTool.Core.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Serilog;
using System.Drawing;

namespace ArcherComparisonTool.Core.Services;

public class ExcelExporter
{
    public async Task ExportComparisonReportAsync(ComparisonReport report, string filePath)
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            
            // Create summary sheet
            CreateSummarySheet(package, report);
            
            // Create sheets for each comparison type
            if (report.ModuleComparisons.Any())
                CreateComparisonSheet(package, "Modules", report.ModuleComparisons);
            
            if (report.FieldComparisons.Any())
                CreateComparisonSheet(package, "Fields", report.FieldComparisons);
            
            if (report.ValuesListComparisons.Any())
                CreateComparisonSheet(package, "Values Lists", report.ValuesListComparisons);
            
            if (report.LayoutComparisons.Any())
                CreateComparisonSheet(package, "Layouts", report.LayoutComparisons);
            
            if (report.DDERuleComparisons.Any())
                CreateComparisonSheet(package, "DDE Rules", report.DDERuleComparisons);
            
            if (report.DDEActionComparisons.Any())
                CreateComparisonSheet(package, "DDE Actions", report.DDEActionComparisons);
            
            if (report.ReportComparisons.Any())
                CreateComparisonSheet(package, "Reports", report.ReportComparisons);
            
            if (report.DashboardComparisons.Any())
                CreateComparisonSheet(package, "Dashboards", report.DashboardComparisons);
            
            if (report.WorkspaceComparisons.Any())
                CreateComparisonSheet(package, "Workspaces", report.WorkspaceComparisons);
            
            if (report.iViewComparisons.Any())
                CreateComparisonSheet(package, "iViews", report.iViewComparisons);
            
            if (report.RoleComparisons.Any())
                CreateComparisonSheet(package, "Roles", report.RoleComparisons);
            
            if (report.SecurityParameterComparisons.Any())
                CreateComparisonSheet(package, "Security Parameters", report.SecurityParameterComparisons);
            
            if (report.NotificationComparisons.Any())
                CreateComparisonSheet(package, "Notifications", report.NotificationComparisons);
            
            if (report.DataFeedComparisons.Any())
                CreateComparisonSheet(package, "Data Feeds", report.DataFeedComparisons);
            
            if (report.ScheduleComparisons.Any())
                CreateComparisonSheet(package, "Schedules", report.ScheduleComparisons);
            
            await package.SaveAsAsync(new FileInfo(filePath));
            Log.Information("Exported comparison report to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export comparison report");
            throw;
        }
    }
    
    private void CreateSummarySheet(ExcelPackage package, ComparisonReport report)
    {
        var worksheet = package.Workbook.Worksheets.Add("Summary");
        
        // Title
        worksheet.Cells["A1"].Value = "Archer Environment Comparison Report";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        
        // Comparison details
        worksheet.Cells["A3"].Value = "Source Environment:";
        worksheet.Cells["B3"].Value = report.SourceEnvironmentName;
        worksheet.Cells["A4"].Value = "Target Environment:";
        worksheet.Cells["B4"].Value = report.TargetEnvironmentName;
        worksheet.Cells["A5"].Value = "Comparison Date:";
        worksheet.Cells["B5"].Value = report.ComparisonDate.ToString("yyyy-MM-dd HH:mm:ss");
        
            // Summary statistics
        var allResults = report.GetAllResults();
        var sourceOnly = allResults.Count(r => r.PropertyName == "Source Only");
        var targetOnly = allResults.Count(r => r.PropertyName == "Target Only");
        var differences = allResults.Count(r => r.Status == ComparisonStatus.Mismatch);
        var matches = allResults.Count(r => r.Status == ComparisonStatus.Match);
        
        worksheet.Cells["A7"].Value = "Summary Statistics";
        worksheet.Cells["A7"].Style.Font.Bold = true;
        worksheet.Cells["A7"].Style.Font.Size = 14;
        
        worksheet.Cells["A9"].Value = "Source Only:";
        worksheet.Cells["B9"].Value = sourceOnly;
        worksheet.Cells["B9"].Style.Font.Color.SetColor(Color.Red);
        
        worksheet.Cells["A10"].Value = "Target Only:";
        worksheet.Cells["B10"].Value = targetOnly;
        worksheet.Cells["B10"].Style.Font.Color.SetColor(Color.Orange);
        
        worksheet.Cells["A11"].Value = "Differences:";
        worksheet.Cells["B11"].Value = differences;
        worksheet.Cells["B11"].Style.Font.Color.SetColor(Color.DarkOrange);

        worksheet.Cells["A12"].Value = "Matches:";
        worksheet.Cells["B12"].Value = matches;
        worksheet.Cells["B12"].Style.Font.Color.SetColor(Color.Green);
        
        worksheet.Cells["A13"].Value = "Total:";
        worksheet.Cells["B13"].Value = allResults.Count;
        worksheet.Cells["B13"].Style.Font.Bold = true;
        
        // Breakdown by type
        worksheet.Cells["A15"].Value = "Breakdown by Type";
        worksheet.Cells["A15"].Style.Font.Bold = true;
        worksheet.Cells["A15"].Style.Font.Size = 14;
        
        int row = 17;
        var groupedByType = allResults.GroupBy(r => r.ComparisonType)
            .OrderByDescending(g => g.Count());
        
        worksheet.Cells[row, 1].Value = "Type";
        worksheet.Cells[row, 2].Value = "Count";
        worksheet.Cells[row, 1, row, 2].Style.Font.Bold = true;
        row++;
        
        foreach (var group in groupedByType)
        {
            worksheet.Cells[row, 1].Value = group.Key.ToString();
            worksheet.Cells[row, 2].Value = group.Count();
            row++;
        }
        
        worksheet.Cells.AutoFitColumns();
    }
    
    private void CreateComparisonSheet(ExcelPackage package, string sheetName, List<ComparisonResult> results)
    {
        var worksheet = package.Workbook.Worksheets.Add(sheetName);
        
        // Headers matching PowerShell output: Report, Module, Level, Field, Property, Source Value, Target Value
        worksheet.Cells["A1"].Value = "Report";
        worksheet.Cells["B1"].Value = "Module";
        worksheet.Cells["C1"].Value = "Level";
        worksheet.Cells["D1"].Value = "Field";
        worksheet.Cells["E1"].Value = "Property";
        worksheet.Cells["F1"].Value = "Source Value";
        worksheet.Cells["G1"].Value = "Target Value";
        
        // Format headers
        using (var range = worksheet.Cells["A1:G1"])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
            range.Style.Font.Color.SetColor(Color.White);
        }
        
        // Data
        int row = 2;
        foreach (var result in results.OrderBy(r => r.ItemName).ThenBy(r => r.ItemIdentifier).ThenBy(r => r.PropertyName))
        {
            worksheet.Cells[row, 1].Value = sheetName; // Report type
            worksheet.Cells[row, 2].Value = result.ItemName; // Module
            worksheet.Cells[row, 3].Value = result.ItemIdentifier; // Level/Identifier
            
            // Field - use SourceValue for Source/Target Only, otherwise use PropertyName context
            if (result.PropertyName == "Source Only" || result.PropertyName == "Target Only")
            {
                worksheet.Cells[row, 4].Value = result.SourceValue?.ToString() ?? "";
            }
            else
            {
                worksheet.Cells[row, 4].Value = result.ParentName;
            }
            
            worksheet.Cells[row, 5].Value = result.PropertyName; // Property
            
            // Source and Target values
            if (result.PropertyName != "Source Only" && result.PropertyName != "Target Only")
            {
                worksheet.Cells[row, 6].Value = result.SourceValue?.ToString() ?? "";
                worksheet.Cells[row, 7].Value = result.TargetValue?.ToString() ?? "";
            }
            
            // Color code rows based on property name (matching PowerShell logic)
            var rowRange = worksheet.Cells[row, 1, row, 7];
            if (result.PropertyName == "Source Only")
            {
                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rowRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 199, 206)); // Red
            }
            else if (result.PropertyName == "Target Only")
            {
                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rowRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 235, 156)); // Orange
            }
            else if (result.Status == ComparisonStatus.Mismatch)
            {
                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rowRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 242, 204)); // Yellow
            }
            else if (result.Status == ComparisonStatus.Match)
            {
                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rowRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(226, 240, 217)); // Light Green
            }
            
            row++;
        }
        
        // Auto-filter
        if (row > 2)
        {
            worksheet.Cells[1, 1, row - 1, 7].AutoFilter = true;
        }
        
        // Freeze top row
        worksheet.View.FreezePanes(2, 1);
        
        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();
    }
}
