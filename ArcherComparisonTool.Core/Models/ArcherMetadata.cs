using ArcherComparisonTool.Core.Models.Metadata;

namespace ArcherComparisonTool.Core.Models;

public class ArcherMetadata
{
    public string EnvironmentName { get; set; } = string.Empty;
    public string ArcherVersion { get; set; } = string.Empty;
    public DateTime CollectionDate { get; set; } = DateTime.Now;
    
    public List<Models.Metadata.Module> Modules { get; set; } = new();
    public List<Models.Metadata.Field> Fields { get; set; } = new();
    public List<Models.Metadata.ValuesList> ValuesLists { get; set; } = new();
    public List<Models.Metadata.Layout> Layouts { get; set; } = new();
    public List<Models.Metadata.LayoutObject> LayoutObjects { get; set; } = new();
    public List<Models.Metadata.DDERule> DDERules { get; set; } = new();
    public List<Models.Metadata.DDEAction> DDEActions { get; set; } = new();
    public List<Models.Metadata.Report> Reports { get; set; } = new();
    public List<Models.Metadata.Dashboard> Dashboards { get; set; } = new();
    public List<Models.Metadata.Workspace> Workspaces { get; set; } = new();
    public List<Models.Metadata.IView> IViews { get; set; } = new();
    public List<Models.Metadata.Role> Roles { get; set; } = new();
    public List<Models.Metadata.SecurityParameter> SecurityParameters { get; set; } = new();
    public List<Models.Metadata.Notification> Notifications { get; set; } = new();
    public List<Models.Metadata.DataFeed> DataFeeds { get; set; } = new();
    public List<Models.Metadata.Schedule> Schedules { get; set; } = new();
}
