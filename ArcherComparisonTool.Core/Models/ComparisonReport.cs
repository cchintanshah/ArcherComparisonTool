namespace ArcherComparisonTool.Core.Models;

public class ComparisonReport
{
    public string SourceEnvironmentName { get; set; } = string.Empty;
    public string TargetEnvironmentName { get; set; } = string.Empty;
    public DateTime ComparisonDate { get; set; } = DateTime.Now;
    
    public List<ComparisonResult> ModuleComparisons { get; set; } = new();
    public List<ComparisonResult> FieldComparisons { get; set; } = new();
    public List<ComparisonResult> ValuesListComparisons { get; set; } = new();
    public List<ComparisonResult> ValuesListValueComparisons { get; set; } = new();
    public List<ComparisonResult> LayoutComparisons { get; set; } = new();
    public List<ComparisonResult> LayoutObjectComparisons { get; set; } = new();
    public List<ComparisonResult> DDERuleComparisons { get; set; } = new();
    public List<ComparisonResult> DDEActionComparisons { get; set; } = new();
    public List<ComparisonResult> ReportComparisons { get; set; } = new();
    public List<ComparisonResult> DashboardComparisons { get; set; } = new();
    public List<ComparisonResult> WorkspaceComparisons { get; set; } = new();
    public List<ComparisonResult> iViewComparisons { get; set; } = new();
    public List<ComparisonResult> RoleComparisons { get; set; } = new();
    public List<ComparisonResult> SecurityParameterComparisons { get; set; } = new();
    public List<ComparisonResult> NotificationComparisons { get; set; } = new();
    public List<ComparisonResult> DataFeedComparisons { get; set; } = new();
    public List<ComparisonResult> ScheduleComparisons { get; set; } = new();
    
    public List<ComparisonResult> GetAllResults()
    {
        var allResults = new List<ComparisonResult>();
        allResults.AddRange(ModuleComparisons);
        allResults.AddRange(FieldComparisons);
        allResults.AddRange(ValuesListComparisons);
        allResults.AddRange(ValuesListValueComparisons);
        allResults.AddRange(LayoutComparisons);
        allResults.AddRange(LayoutObjectComparisons);
        allResults.AddRange(DDERuleComparisons);
        allResults.AddRange(DDEActionComparisons);
        allResults.AddRange(ReportComparisons);
        allResults.AddRange(DashboardComparisons);
        allResults.AddRange(WorkspaceComparisons);
        allResults.AddRange(iViewComparisons);
        allResults.AddRange(RoleComparisons);
        allResults.AddRange(SecurityParameterComparisons);
        allResults.AddRange(NotificationComparisons);
        allResults.AddRange(DataFeedComparisons);
        allResults.AddRange(ScheduleComparisons);
        return allResults;
    }
}
