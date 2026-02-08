namespace ArcherComparisonTool.Core.Models;

public class ComparisonResult
{
    public ComparisonType ComparisonType { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string ItemIdentifier { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public object? SourceValue { get; set; }
    public object? TargetValue { get; set; }
    public ComparisonStatus Status { get; set; }
    public Severity Severity { get; set; }
    
    // For backward compatibility
    public string ParentName { get; set; } = string.Empty;
    public List<PropertyDifference> Differences { get; set; } = new();
}

public class PropertyDifference
{
    public string PropertyName { get; set; } = string.Empty;
    public object? SourceValue { get; set; }
    public object? TargetValue { get; set; }
}

public enum ComparisonType
{
    Module,
    Field,
    ValuesList,
    ValuesListValue,
    Layout,
    LayoutObject,
    DDERule,
    DDEAction,
    Report,
    Dashboard,
    Workspace,
    IView,
    Role,
    SecurityParameter,
    Notification,
    DataFeed,
    Schedule
}

public enum ComparisonStatus
{
    Match,
    Mismatch,
    MissingInSource,
    MissingInTarget
}

public enum Severity
{
    Info,
    Warning,
    Critical
}
