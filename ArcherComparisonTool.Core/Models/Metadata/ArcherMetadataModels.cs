using System.Text.Json.Serialization;

namespace ArcherComparisonTool.Core.Models.Metadata;

public class Module
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Guid { get; set; }
    public string? Alias { get; set; }
    public string? Type { get; set; }
    public string? StatusLabel { get; set; }
    public string? TargetApplication { get; set; }
    public bool IsLeveled { get; set; }
    public bool IsSystem { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class Field
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Properties for comparison matching PowerShell script
    public string? Module { get; set; }
    public string? Level { get; set; }
    
    public string? Guid { get; set; }
    public string? Alias { get; set; }
    public int LevelId { get; set; }
    public string? TypeLabel { get; set; }
    public string? Access { get; set; }
    public bool IsActive { get; set; }
    public bool IsRequired { get; set; }
    public bool IsCalculated { get; set; }
    public string? Formula { get; set; }
    public int? RelatedValuesListId { get; set; }
    public string? HelpText { get; set; }
}

public class ValuesList
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Guid { get; set; }
    public string? Alias { get; set; }
    public int LevelId { get; set; }
    public int? RelatedValuesListId { get; set; }
    public bool IsActive { get; set; }
    public List<ValuesListValue> Values { get; set; } = new();
}

public class ValuesListValue
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ValuesListId { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public List<ValuesListValue> Children { get; set; } = new();
}

public class Layout
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Properties for comparison matching PowerShell script
    public string? Module { get; set; }
    public string? Level { get; set; }
    public string? LayoutName { get; set; }
    public string? LayoutTab { get; set; }
    public string? LayoutSection { get; set; }
    public string? LayoutField { get; set; }
    public string? LayoutType { get; set; }
    
    public string? Guid { get; set; }
    public int LevelId { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
}

public class LayoutObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LayoutId { get; set; }
    public string? ObjectType { get; set; }
    public int Depth { get; set; }
}

public class DDERule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Guid { get; set; }
    public int LayoutId { get; set; }
    public bool IsActive { get; set; }
    public int ExecutionOrder { get; set; }
}

public class DDEAction
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Guid { get; set; }
    public int LayoutId { get; set; }
    public bool IsActive { get; set; }
    public string? TypeLabel { get; set; }
}

public class Report
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ModuleName { get; set; }
    public string? ReportTypeDisplayColumnString { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
}

public class Dashboard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
}

public class Workspace
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class IView
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Guid { get; set; }
    public string? IViewFolderName { get; set; }
    public string? TypeString { get; set; }
    public bool IsActive { get; set; }
}

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Guid { get; set; }
    public string? Alias { get; set; }
    public bool IsSysAdmin { get; set; }
}

public class SecurityParameter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class Notification
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ApplicationName { get; set; }
    public bool Active { get; set; }
    public string? TypeDisplayText { get; set; }
}

public class DataFeed
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Guid { get; set; }
    public bool IsActive { get; set; }
    public string? Target { get; set; }
}

public class Schedule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Guid { get; set; }
    public string? ModuleName { get; set; }
    public bool IsActive { get; set; }
    public string? Frequency { get; set; }
}
