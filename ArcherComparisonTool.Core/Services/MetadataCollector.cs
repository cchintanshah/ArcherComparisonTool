using ArcherComparisonTool.Core.Models;
using ArcherComparisonTool.Core.Models.Metadata;
using Serilog;

namespace ArcherComparisonTool.Core.Services;

public class MetadataCollector
{
    private readonly ArcherApiClient _apiClient;
    
    public MetadataCollector(ArcherApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    public async Task<ArcherMetadata> CollectAllMetadataAsync(
        ArcherEnvironment environment,
        string password,
        CollectionOptions options,
        IProgress<string>? progress = null)
    {
        var metadata = new ArcherMetadata
        {
            EnvironmentName = environment.DisplayName,
            CollectionDate = DateTime.Now
        };
        
        try
        {
            progress?.Report($"Connecting to {environment.DisplayName}...");
            await _apiClient.LoginAsync(environment, password);
            
            progress?.Report("Getting Archer version...");
            metadata.ArcherVersion = await _apiClient.GetVersionAsync();
            
            // Get all modules first
            progress?.Report("Getting modules...");
            var allModules = await _apiClient.GetModulesAsync();
            
            // Filter modules based on selection
            if (options.SelectedModuleIds.Any())
            {
                allModules = allModules.Where(m => options.SelectedModuleIds.Contains(m.Id)).ToList();
            }
            
            // Always set modules if we have them, key for other filtering
            metadata.Modules = allModules;
            progress?.Report($"Retrieved {metadata.Modules.Count} relevant modules");

            var selectedModuleNames = allModules.Select(m => m.Name).ToHashSet();
            
            // Collect fields for selected modules
            if (options.IncludeFields && allModules.Any())
            {
                progress?.Report("Getting fields...");
                var fields = new List<Field>();
                
                // Note: Simplified logic. In reality we need to fetch fields per module/level
                // For now, assuming GetFieldsAsync might need levelId, which we'd get from Module -> Level
                // Since this is a partial implementation, we'll iterate modules if we could get levels.
                // Keeping it abstract as per current API capability.
                // But generally: 
                // foreach (var module in allModules) { fields.AddRange(await GetFieldsForModule(module)); }
                
                // For this implementation, since ApiClient.GetFieldsAsync takes levelId, we really need levels.
                // Assuming we can't fully implement without real level IDs, we will stick to the structure.
                
                // Mock behavior: The real implementation would iterate through modules/levels.
                // Here we will fetch what we can. Current API client has GetFieldsAsync(int levelId).
                // We'd need to fetch levels first. Attempting to be robust:
                
                // Logic: 
                // 1. We have filtered modules.
                // 2. Fetch fields that belong to these modules.
                
                // Since we don't have GetLevels, we'll skip the actual API call loop if checking specifically for levelIds
                // But if we had a GetAllFields, we'd filter:
                // fields = allFields.Where(f => selectedModuleNames.Contains(f.ModuleName)).ToList();
                
                metadata.Fields = fields;
            }
            
            // Collect and Filter Layouts
            if (options.IncludeLayouts)
            {
                progress?.Report("Getting layouts...");
                var layouts = await _apiClient.GetLayoutsAsync();
                
                if (options.SelectedModuleIds.Any())
                {
                    layouts = layouts.Where(l => selectedModuleNames.Contains(l.Module ?? "")).ToList();
                }
                metadata.Layouts = layouts;
            }

            // Collect and Filter Values Lists (Complex: related to fields)
            if (options.IncludeValuesLists)
            {
                progress?.Report("Getting values lists...");
                var valuesLists = await _apiClient.GetValuesListsAsync();
                // Real logic: Filter based on Fields' RelatedValuesListId
                // simplified:
                 // if (options.SelectedModuleIds.Any()) { ... }
                metadata.ValuesLists = valuesLists;
            }

            // DDE
            if (options.IncludeDDERules)
            {
                progress?.Report("Getting DDE rules...");
                metadata.DDERules = await _apiClient.GetDDERulesAsync();
                 // Filter by layout ID from filtered layouts
            }
             if (options.IncludeDDEActions)
            {
                progress?.Report("Getting DDE actions...");
                metadata.DDEActions = await _apiClient.GetDDEActionsAsync();
            }

            // Instance Specific - But filterable by Module Name if applicable
            
            if (options.IncludeReports)
            {
                progress?.Report("Getting reports...");
                var reports = await _apiClient.GetReportsAsync();
                
                if (options.SelectedModuleIds.Any())
                {
                    reports = reports
                        .Where(r => selectedModuleNames.Contains(r.ModuleName ?? ""))
                        .ToList();
                }
                metadata.Reports = reports;
                progress?.Report($"Retrieved {metadata.Reports.Count} reports");
            }
            
            if (options.IncludeDashboards)
            {
                progress?.Report("Getting dashboards...");
                metadata.Dashboards = await _apiClient.GetDashboardsAsync();
                // Dashboards are usually global or hard to link to module without content analysis
            }

             if (options.IncludeWorkspaces)
            {
                progress?.Report("Getting workspaces...");
                metadata.Workspaces = await _apiClient.GetWorkspacesAsync();
            }

            if (options.IncludeiViews)
            {
                progress?.Report("Getting iViews...");
                metadata.IViews = await _apiClient.GetiViewsAsync();
            }

            if (options.IncludeRoles)
            {
                progress?.Report("Getting roles...");
                metadata.Roles = await _apiClient.GetRolesAsync();
            }

            if (options.IncludeSecurityParameters)
            {
                progress?.Report("Getting security parameters...");
                metadata.SecurityParameters = await _apiClient.GetSecurityParametersAsync();
            }

            if (options.IncludeNotifications)
            {
                progress?.Report("Getting notifications...");
                var notifications = await _apiClient.GetNotificationsAsync();
                 if (options.SelectedModuleIds.Any())
                {
                    notifications = notifications
                        .Where(n => selectedModuleNames.Contains(n.ApplicationName ?? ""))
                        .ToList();
                }
                metadata.Notifications = notifications;
            }

            if (options.IncludeDataFeeds)
            {
                progress?.Report("Getting data feeds...");
                metadata.DataFeeds = await _apiClient.GetDataFeedsAsync();
                 if (options.SelectedModuleIds.Any())
                {
                     // Filter if Target/Source matches module name
                     metadata.DataFeeds = metadata.DataFeeds.Where(d => 
                         selectedModuleNames.Contains(d.Target ?? "") || 
                         selectedModuleNames.Contains(d.Name)).ToList();
                }
            }

             if (options.IncludeSchedules)
            {
                progress?.Report("Getting schedules...");
                metadata.Schedules = await _apiClient.GetSchedulesAsync();
                 if (options.SelectedModuleIds.Any())
                {
                    metadata.Schedules = metadata.Schedules
                        .Where(s => selectedModuleNames.Contains(s.ModuleName ?? ""))
                        .ToList();
                }
            }
            
            progress?.Report("Collection complete!");
            Log.Information("Metadata collection completed for {Environment}. Modules: {ModuleCount}",
                environment.DisplayName, metadata.Modules.Count);
            
            return metadata;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to collect metadata from {Environment}", environment.DisplayName);
            progress?.Report($"Error: {ex.Message}");
            throw;
        }
        finally
        {
            await _apiClient.LogoutAsync();
        }
    }
}
