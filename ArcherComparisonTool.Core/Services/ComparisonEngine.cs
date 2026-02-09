using System.Reflection;
using ArcherComparisonTool.Core.Models;
using ArcherComparisonTool.Core.Models.Metadata;
using Serilog;

namespace ArcherComparisonTool.Core.Services;

public class ComparisonEngine
{
    // Properties to ignore in comparisons (matching PowerShell script)
    private static readonly HashSet<string> FieldPropsToIgnore = new()
    {
        "Id", "ModuleId", "LevelId", "FieldId", "Guid", "ReferencedLevel",
        "DisplayFieldInGlobalReports", "DisplayFieldInPersonalReports", 
        "StatsFieldInStatisticalReports"
    };
    
    private static readonly HashSet<string> LayoutPropsToIgnore = new()
    {
        "Id"
    };
    
    private static readonly HashSet<string> CommonPropsToIgnore = new()
    {
        "Id"
    };
    
    public ComparisonReport CompareEnvironments(
        ArcherMetadata source,
        ArcherMetadata target,
        CollectionOptions options)
    {
        var report = new ComparisonReport
        {
            SourceEnvironmentName = source.EnvironmentName,
            TargetEnvironmentName = target.EnvironmentName,
            ComparisonDate = DateTime.Now
        };
        
        var tasks = new List<Task>();
        
        // Compare only selected metadata types in parallel
        if (options.IncludeModules)
        {
            tasks.Add(Task.Run(() =>
            {
                report.ModuleComparisons = CompareModules(source.Modules, target.Modules);
            }));
        }
        
        if (options.IncludeFields)
        {
            tasks.Add(Task.Run(() =>
            {
                report.FieldComparisons = CompareFields(source.Fields, target.Fields);
            }));
        }
        
        if (options.IncludeLayouts)
        {
            tasks.Add(Task.Run(() =>
            {
                report.LayoutComparisons = CompareLayouts(source.Layouts, target.Layouts);
            }));
        }
        
        if (options.IncludeValuesLists)
        {
            tasks.Add(Task.Run(() =>
            {
                report.ValuesListComparisons = CompareValuesLists(source.ValuesLists, target.ValuesLists, options.MaxDepth);
            }));
        }
        
        if (options.IncludeReports)
        {
            tasks.Add(Task.Run(() =>
            {
                report.ReportComparisons = CompareReports(source.Reports, target.Reports);
            }));
        }
        
        if (options.IncludeDashboards)
        {
            tasks.Add(Task.Run(() =>
            {
                report.DashboardComparisons = CompareDashboards(source.Dashboards, target.Dashboards);
            }));
        }
        
        // Wait for all comparisons to complete
        Task.WaitAll(tasks.ToArray());
        
        Log.Information("Comparison completed. Total results: {Count}", report.GetAllResults().Count);
        
        return report;
    }
    
    public List<ComparisonResult> CompareModules(List<Models.Metadata.Module> source, List<Models.Metadata.Module> target)
    {
        var results = new List<ComparisonResult>();
        
        // Use Name as composite key (Module name is unique)
        var sourceDict = source
            .Where(m => !string.IsNullOrEmpty(m.Name))
            .GroupBy(m => m.Name)
            .ToDictionary(g => g.Key, g => g.First());
            
        var targetDict = target
            .Where(m => !string.IsNullOrEmpty(m.Name))
            .GroupBy(m => m.Name)
            .ToDictionary(g => g.Key, g => g.First());
        
        // Missing in target (Source Only)
        foreach (var key in sourceDict.Keys.Except(targetDict.Keys))
        {
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.Module,
                ItemName = key,
                ItemIdentifier = key,
                PropertyName = "Source Only",
                Status = ComparisonStatus.MissingInTarget,
                Severity = Severity.Warning
            });
        }
        
        // Missing in source (Target Only)
        foreach (var key in targetDict.Keys.Except(sourceDict.Keys))
        {
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.Module,
                ItemName = key,
                ItemIdentifier = key,
                PropertyName = "Target Only",
                Status = ComparisonStatus.MissingInSource,
                Severity = Severity.Info
            });
        }
        
        // Compare common items property by property
        foreach (var key in sourceDict.Keys.Intersect(targetDict.Keys))
        {
            var sourceItem = sourceDict[key];
            var targetItem = targetDict[key];
            
            var propertyDiffs = CompareObjectProperties(sourceItem, targetItem, CommonPropsToIgnore);
            
            if (propertyDiffs.Count > 0)
            {
                foreach (var diff in propertyDiffs)
                {
                    results.Add(new ComparisonResult
                    {
                        ComparisonType = ComparisonType.Module,
                        ItemName = key,
                        ItemIdentifier = key,
                        PropertyName = diff.PropertyName,
                        SourceValue = diff.SourceValue,
                        TargetValue = diff.TargetValue,
                        Status = ComparisonStatus.Mismatch,
                        Severity = Severity.Warning
                    });
                }
            }
            else
            {
                // Match
                results.Add(new ComparisonResult
                {
                    ComparisonType = ComparisonType.Module,
                    ItemName = key,
                    ItemIdentifier = key,
                    PropertyName = "All Properties",
                    Status = ComparisonStatus.Match,
                    Severity = Severity.Info
                });
            }
        }
        
        return results;
    }
    
    public List<ComparisonResult> CompareFields(List<Models.Metadata.Field> source, List<Models.Metadata.Field> target)
    {
        var results = new List<ComparisonResult>();
        
        // Composite key: Module + Level + Field (matching PowerShell script)
        var sourceDict = source
            .Where(f => !string.IsNullOrEmpty(f.Module) && !string.IsNullOrEmpty(f.Level) && !string.IsNullOrEmpty(f.Name))
            .GroupBy(f => $"{f.Module}|{f.Level}|{f.Name}")
            .ToDictionary(g => g.Key, g => g.First());
            
        var targetDict = target
            .Where(f => !string.IsNullOrEmpty(f.Module) && !string.IsNullOrEmpty(f.Level) && !string.IsNullOrEmpty(f.Name))
            .GroupBy(f => $"{f.Module}|{f.Level}|{f.Name}")
            .ToDictionary(g => g.Key, g => g.First());
        
        // Missing in target (Source Only)
        foreach (var key in sourceDict.Keys.Except(targetDict.Keys))
        {
            var field = sourceDict[key];
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.Field,
                ItemName = field.Module,
                ItemIdentifier = field.Level,
                PropertyName = "Source Only",
                SourceValue = field.Name,
                Status = ComparisonStatus.MissingInTarget,
                Severity = Severity.Critical
            });
        }
        
        // Missing in source (Target Only)
        foreach (var key in targetDict.Keys.Except(sourceDict.Keys))
        {
            var field = targetDict[key];
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.Field,
                ItemName = field.Module,
                ItemIdentifier = field.Level,
                PropertyName = "Target Only",
                SourceValue = field.Name,
                Status = ComparisonStatus.MissingInSource,
                Severity = Severity.Info
            });
        }
        
        // Compare common items property by property
        foreach (var key in sourceDict.Keys.Intersect(targetDict.Keys))
        {
            var sourceField = sourceDict[key];
            var targetField = targetDict[key];
            
            var propertyDiffs = CompareObjectProperties(sourceField, targetField, FieldPropsToIgnore);
            
            if (propertyDiffs.Count > 0)
            {
                foreach (var diff in propertyDiffs)
                {
                    results.Add(new ComparisonResult
                    {
                        ComparisonType = ComparisonType.Field,
                        ItemName = sourceField.Module,
                        ItemIdentifier = sourceField.Level,
                        PropertyName = diff.PropertyName,
                        SourceValue = diff.SourceValue,
                        TargetValue = diff.TargetValue,
                        Status = ComparisonStatus.Mismatch,
                        Severity = Severity.Warning
                    });
                }
            }
            else
            {
                // Match
                results.Add(new ComparisonResult
                {
                    ComparisonType = ComparisonType.Field,
                    ItemName = sourceField.Module,
                    ItemIdentifier = sourceField.Level,
                    PropertyName = "All Properties",
                    SourceValue = sourceField.Name,
                    Status = ComparisonStatus.Match,
                    Severity = Severity.Info
                });
            }
        }
        
        return results;
    }
    
    public List<ComparisonResult> CompareLayouts(List<Models.Metadata.Layout> source, List<Models.Metadata.Layout> target)
    {
        var results = new List<ComparisonResult>();
        
        // Remove placeholders and available items (matching PowerShell)
        source = source.Where(l => l.LayoutType != "Placeholder" && l.LayoutTab != "Available").ToList();
        target = target.Where(l => l.LayoutType != "Placeholder" && l.LayoutTab != "Available").ToList();
        
        // Composite key: Module + Level + Layout + LayoutTab + LayoutSection + LayoutField
        var sourceDict = source
            .Where(l => !string.IsNullOrEmpty(l.Module))
            .GroupBy(l => $"{l.Module}|{l.Level}|{l.LayoutName}|{l.LayoutTab}|{l.LayoutSection}|{l.LayoutField}")
            .ToDictionary(g => g.Key, g => g.First());
            
        var targetDict = target
            .Where(l => !string.IsNullOrEmpty(l.Module))
            .GroupBy(l => $"{l.Module}|{l.Level}|{l.LayoutName}|{l.LayoutTab}|{l.LayoutSection}|{l.LayoutField}")
            .ToDictionary(g => g.Key, g => g.First());
        
        // Missing in target (Source Only)
        foreach (var key in sourceDict.Keys.Except(targetDict.Keys))
        {
            var layout = sourceDict[key];
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.Layout,
                ItemName = layout.Module,
                ItemIdentifier = $"{layout.Level} > {layout.LayoutName} > {layout.LayoutTab} > {layout.LayoutSection}",
                PropertyName = "Source Only",
                SourceValue = layout.LayoutField,
                Status = ComparisonStatus.MissingInTarget,
                Severity = Severity.Warning
            });
        }
        
        // Missing in source (Target Only)
        foreach (var key in targetDict.Keys.Except(sourceDict.Keys))
        {
            var layout = targetDict[key];
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.Layout,
                ItemName = layout.Module,
                ItemIdentifier = $"{layout.Level} > {layout.LayoutName} > {layout.LayoutTab} > {layout.LayoutSection}",
                PropertyName = "Target Only",
                SourceValue = layout.LayoutField,
                Status = ComparisonStatus.MissingInSource,
                Severity = Severity.Info
            });
        }
        
        // Compare common items property by property
        foreach (var key in sourceDict.Keys.Intersect(targetDict.Keys))
        {
            var sourceLayout = sourceDict[key];
            var targetLayout = targetDict[key];
            
            var propertyDiffs = CompareObjectProperties(sourceLayout, targetLayout, LayoutPropsToIgnore);
            
            if (propertyDiffs.Count > 0)
            {
                foreach (var diff in propertyDiffs)
                {
                    results.Add(new ComparisonResult
                    {
                        ComparisonType = ComparisonType.Layout,
                        ItemName = sourceLayout.Module,
                        ItemIdentifier = $"{sourceLayout.Level} > {sourceLayout.LayoutName} > {sourceLayout.LayoutTab} > {sourceLayout.LayoutSection}",
                        PropertyName = diff.PropertyName,
                        SourceValue = diff.SourceValue,
                        TargetValue = diff.TargetValue,
                        Status = ComparisonStatus.Mismatch,
                        Severity = Severity.Warning
                    });
                }
            }
            else
            {
                 // Match
                results.Add(new ComparisonResult
                {
                    ComparisonType = ComparisonType.Layout,
                    ItemName = sourceLayout.Module,
                    ItemIdentifier = $"{sourceLayout.Level} > {sourceLayout.LayoutName} > {sourceLayout.LayoutTab} > {sourceLayout.LayoutSection}",
                    PropertyName = "All Properties",
                    SourceValue = sourceLayout.LayoutField,
                    Status = ComparisonStatus.Match,
                    Severity = Severity.Info
                });
            }
        }
        
        return results;
    }
    
    public List<ComparisonResult> CompareValuesLists(List<Models.Metadata.ValuesList> source, List<Models.Metadata.ValuesList> target, int maxDepth)
    {
        var results = new List<ComparisonResult>();
        
        var sourceDict = source
            .Where(v => !string.IsNullOrEmpty(v.Name))
            .GroupBy(v => v.Name)
            .ToDictionary(g => g.Key, g => g.First());
            
        var targetDict = target
            .Where(v => !string.IsNullOrEmpty(v.Name))
            .GroupBy(v => v.Name)
            .ToDictionary(g => g.Key, g => g.First());
        
        // Missing in target
        foreach (var key in sourceDict.Keys.Except(targetDict.Keys))
        {
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.ValuesList,
                ItemName = key,
                PropertyName = "Source Only",
                Status = ComparisonStatus.MissingInTarget,
                Severity = Severity.Warning
            });
        }
        
        // Missing in source
        foreach (var key in targetDict.Keys.Except(sourceDict.Keys))
        {
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.ValuesList,
                ItemName = key,
                PropertyName = "Target Only",
                Status = ComparisonStatus.MissingInSource,
                Severity = Severity.Info
            });
        }
        
        // Compare common items
        foreach (var key in sourceDict.Keys.Intersect(targetDict.Keys))
        {
            var sourceItem = sourceDict[key];
            var targetItem = targetDict[key];
            
            var propertyDiffs = CompareObjectProperties(sourceItem, targetItem, new HashSet<string> { "Id", "Values" });
            
            if (propertyDiffs.Count > 0)
            {
                foreach (var diff in propertyDiffs)
                {
                    results.Add(new ComparisonResult
                    {
                        ComparisonType = ComparisonType.ValuesList,
                        ItemName = key,
                        PropertyName = diff.PropertyName,
                        SourceValue = diff.SourceValue,
                        TargetValue = diff.TargetValue,
                        Status = ComparisonStatus.Mismatch,
                        Severity = Severity.Warning
                    });
                }
            }
            else
            {
                // Match (at top level)
                results.Add(new ComparisonResult
                {
                    ComparisonType = ComparisonType.ValuesList,
                    ItemName = key,
                    PropertyName = "All Properties",
                    Status = ComparisonStatus.Match,
                    Severity = Severity.Info
                });
            }
            
            // Compare values recursively
            if (maxDepth > 0)
            {
                var valueResults = CompareValuesListValues(
                    sourceItem.Values,
                    targetItem.Values,
                    maxDepth,
                    key
                );
                results.AddRange(valueResults);
            }
        }
        
        return results;
    }
    
    private List<ComparisonResult> CompareValuesListValues(
        List<Models.Metadata.ValuesListValue> source,
        List<Models.Metadata.ValuesListValue> target,
        int depth,
        string parentName)
    {
        if (depth <= 0) return new List<ComparisonResult>();
        
        var results = new List<ComparisonResult>();
        
        var sourceDict = source
            .Where(v => !string.IsNullOrEmpty(v.Name))
            .GroupBy(v => v.Name)
            .ToDictionary(g => g.Key, g => g.First());
            
        var targetDict = target
            .Where(v => !string.IsNullOrEmpty(v.Name))
            .GroupBy(v => v.Name)
            .ToDictionary(g => g.Key, g => g.First());
        
        // Missing in target
        foreach (var key in sourceDict.Keys.Except(targetDict.Keys))
        {
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.ValuesListValue,
                ItemName = parentName,
                ItemIdentifier = key,
                PropertyName = "Source Only",
                Status = ComparisonStatus.MissingInTarget,
                Severity = Severity.Warning
            });
        }
        
        // Missing in source
        foreach (var key in targetDict.Keys.Except(sourceDict.Keys))
        {
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.ValuesListValue,
                ItemName = parentName,
                ItemIdentifier = key,
                PropertyName = "Target Only",
                Status = ComparisonStatus.MissingInSource,
                Severity = Severity.Info
            });
        }
        
        // Compare common items and recurse into children
        foreach (var key in sourceDict.Keys.Intersect(targetDict.Keys))
        {
            var sourceItem = sourceDict[key];
            var targetItem = targetDict[key];
            
            var propertyDiffs = CompareObjectProperties(sourceItem, targetItem, new HashSet<string> { "Id", "Children" });
            
            if (propertyDiffs.Count > 0)
            {
                foreach (var diff in propertyDiffs)
                {
                    results.Add(new ComparisonResult
                    {
                        ComparisonType = ComparisonType.ValuesListValue,
                        ItemName = parentName,
                        ItemIdentifier = key,
                        PropertyName = diff.PropertyName,
                        SourceValue = diff.SourceValue,
                        TargetValue = diff.TargetValue,
                        Status = ComparisonStatus.Mismatch,
                        Severity = Severity.Info
                    });
                }
            }
            else
            {
                 // Match
                results.Add(new ComparisonResult
                {
                    ComparisonType = ComparisonType.ValuesListValue,
                    ItemName = parentName,
                    ItemIdentifier = key,
                    PropertyName = "All Properties",
                    Status = ComparisonStatus.Match,
                    Severity = Severity.Info
                });
            }
            
            // Recurse into children
            if (sourceItem.Children.Any() || targetItem.Children.Any())
            {
                var childResults = CompareValuesListValues(
                    sourceItem.Children,
                    targetItem.Children,
                    depth - 1,
                    $"{parentName} > {key}"
                );
                results.AddRange(childResults);
            }
        }
        
        return results;
    }
    
    public List<ComparisonResult> CompareReports(List<Models.Metadata.Report> source, List<Models.Metadata.Report> target)
    {
        var results = new List<ComparisonResult>();
        
        var sourceDict = source
            .Where(r => !string.IsNullOrEmpty(r.Name))
            .GroupBy(r => r.Name)
            .ToDictionary(g => g.Key, g => g.First());
            
        var targetDict = target
            .Where(r => !string.IsNullOrEmpty(r.Name))
            .GroupBy(r => r.Name)
            .ToDictionary(g => g.Key, g => g.First());
        
        // Missing in target
        foreach (var key in sourceDict.Keys.Except(targetDict.Keys))
        {
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.Report,
                ItemName = key,
                PropertyName = "Source Only",
                Status = ComparisonStatus.MissingInTarget,
                Severity = Severity.Warning
            });
        }
        
        // Missing in source
        foreach (var key in targetDict.Keys.Except(sourceDict.Keys))
        {
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.Report,
                ItemName = key,
                PropertyName = "Target Only",
                Status = ComparisonStatus.MissingInSource,
                Severity = Severity.Info
            });
        }
        
        // Compare common items
        foreach (var key in sourceDict.Keys.Intersect(targetDict.Keys))
        {
            var sourceItem = sourceDict[key];
            var targetItem = targetDict[key];
            
            var propertyDiffs = CompareObjectProperties(sourceItem, targetItem, CommonPropsToIgnore);
            
            if (propertyDiffs.Count > 0)
            {
                foreach (var diff in propertyDiffs)
                {
                    results.Add(new ComparisonResult
                    {
                        ComparisonType = ComparisonType.Report,
                        ItemName = key,
                        PropertyName = diff.PropertyName,
                        SourceValue = diff.SourceValue,
                        TargetValue = diff.TargetValue,
                        Status = ComparisonStatus.Mismatch,
                        Severity = Severity.Info
                    });
                }
            }
            else
            {
                // Match
                results.Add(new ComparisonResult
                {
                    ComparisonType = ComparisonType.Report,
                    ItemName = key,
                    PropertyName = "All Properties",
                    Status = ComparisonStatus.Match,
                    Severity = Severity.Info
                });
            }
        }
        
        return results;
    }
    
    public List<ComparisonResult> CompareDashboards(List<Models.Metadata.Dashboard> source, List<Models.Metadata.Dashboard> target)
    {
        var results = new List<ComparisonResult>();
        
        var sourceDict = source
            .Where(d => !string.IsNullOrEmpty(d.Name))
            .GroupBy(d => d.Alias ?? d.Name)
            .ToDictionary(g => g.Key, g => g.First());
            
        var targetDict = target
            .Where(d => !string.IsNullOrEmpty(d.Name))
            .GroupBy(d => d.Alias ?? d.Name)
            .ToDictionary(g => g.Key, g => g.First());
        
        // Missing in target
        foreach (var key in sourceDict.Keys.Except(targetDict.Keys))
        {
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.Dashboard,
                ItemName = key,
                PropertyName = "Source Only",
                Status = ComparisonStatus.MissingInTarget,
                Severity = Severity.Warning
            });
        }
        
        // Missing in source
        foreach (var key in targetDict.Keys.Except(sourceDict.Keys))
        {
            results.Add(new ComparisonResult
            {
                ComparisonType = ComparisonType.Dashboard,
                ItemName = key,
                PropertyName = "Target Only",
                Status = ComparisonStatus.MissingInSource,
                Severity = Severity.Info
            });
        }
        
        // Compare common items
        foreach (var key in sourceDict.Keys.Intersect(targetDict.Keys))
        {
            var sourceItem = sourceDict[key];
            var targetItem = targetDict[key];
            
            var propertyDiffs = CompareObjectProperties(sourceItem, targetItem, CommonPropsToIgnore);
            
            if (propertyDiffs.Count > 0)
            {
                foreach (var diff in propertyDiffs)
                {
                    results.Add(new ComparisonResult
                    {
                        ComparisonType = ComparisonType.Dashboard,
                        ItemName = key,
                        PropertyName = diff.PropertyName,
                        SourceValue = diff.SourceValue,
                        TargetValue = diff.TargetValue,
                        Status = ComparisonStatus.Mismatch,
                        Severity = Severity.Info
                    });
                }
            }
            else
            {
                // Match
                results.Add(new ComparisonResult
                {
                    ComparisonType = ComparisonType.Dashboard,
                    ItemName = key,
                    PropertyName = "All Properties",
                    Status = ComparisonStatus.Match,
                    Severity = Severity.Info
                });
            }
        }
        
        return results;
    }
    
    private List<PropertyDifference> CompareObjectProperties<T>(T source, T target, HashSet<string> excludeProperties) where T : class
    {
        var differences = new List<PropertyDifference>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var prop in properties)
        {
            // Skip excluded properties
            if (excludeProperties.Contains(prop.Name)) continue;
            
            // Skip collection properties
            if (prop.PropertyType.IsGenericType && 
                prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                continue;
            }
            
            var sourceValue = prop.GetValue(source);
            var targetValue = prop.GetValue(target);
            
            // Convert to string for comparison (matching PowerShell .ToString() behavior)
            var sourceStr = sourceValue?.ToString() ?? "";
            var targetStr = targetValue?.ToString() ?? "";
            
            if (sourceStr != targetStr)
            {
                differences.Add(new PropertyDifference
                {
                    PropertyName = prop.Name,
                    SourceValue = sourceValue,
                    TargetValue = targetValue
                });
            }
        }
        
        return differences;
    }
}
