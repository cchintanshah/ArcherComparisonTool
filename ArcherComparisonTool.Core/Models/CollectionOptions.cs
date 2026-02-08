namespace ArcherComparisonTool.Core.Models;

public class CollectionOptions
{
    public List<int> SelectedModuleIds { get; set; } = new();
    public bool IncludeModules { get; set; } = true;
    public bool IncludeFields { get; set; } = true;
    public bool IncludeValuesLists { get; set; } = true;
    public bool IncludeLayouts { get; set; } = true;
    public bool IncludeDDERules { get; set; } = true;
    public bool IncludeDDEActions { get; set; } = true;
    public bool IncludeReports { get; set; } = true;
    public bool IncludeDashboards { get; set; } = true;
    public bool IncludeWorkspaces { get; set; } = true;
    public bool IncludeiViews { get; set; } = true;
    public bool IncludeRoles { get; set; } = true;
    public bool IncludeSecurityParameters { get; set; } = true;
    public bool IncludeNotifications { get; set; } = true;
    public bool IncludeDataFeeds { get; set; } = true;
    public bool IncludeSchedules { get; set; } = true;
    public int MaxDepth { get; set; } = 10;
}
