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
            
            if (options.IncludeModules)
            {
                metadata.Modules = allModules;
                progress?.Report($"Retrieved {metadata.Modules.Count} modules");
            }
            
            // Collect fields for selected modules
            if (options.IncludeFields && allModules.Any())
            {
                progress?.Report("Getting fields...");
                var fields = new List<Field>();
                
                foreach (var module in allModules)
                {
                    // In real implementation, would need to get levels from module
                    // For now, simplified
                    progress?.Report($"Getting fields for {module.Name}...");
                }
                
                metadata.Fields = fields;
            }
            
            // Collect other metadata types
            if (options.IncludeReports)
            {
                progress?.Report("Getting reports...");
                metadata.Reports = await _apiClient.GetReportsAsync();
                
                // Filter by selected modules
                if (options.SelectedModuleIds.Any())
                {
                    var moduleNames = allModules.Select(m => m.Name).ToHashSet();
                    metadata.Reports = metadata.Reports
                        .Where(r => moduleNames.Contains(r.ModuleName ?? ""))
                        .ToList();
                }
                
                progress?.Report($"Retrieved {metadata.Reports.Count} reports");
            }
            
            if (options.IncludeDashboards)
            {
                progress?.Report("Getting dashboards...");
                metadata.Dashboards = await _apiClient.GetDashboardsAsync();
                progress?.Report($"Retrieved {metadata.Dashboards.Count} dashboards");
            }
            
            // Add similar collection for other metadata types...
            
            progress?.Report("Collection complete!");
            Log.Information("Metadata collection completed for {Environment}. Modules: {ModuleCount}, Reports: {ReportCount}",
                environment.DisplayName, metadata.Modules.Count, metadata.Reports.Count);
            
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
