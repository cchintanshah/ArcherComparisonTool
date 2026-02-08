using ArcherComparisonTool.Core.Models;
using ArcherComparisonTool.Core.Models.Metadata;

namespace ArcherComparisonTool.Core.Services;

public class MockMetadataService : IMetadataService
{
    public Task<List<Module>> GetModulesAsync(ArcherEnvironment environment, string password)
    {
        // Mock modules
        var modules = new List<Module>
        {
            new Module { Id = 1, Name = "Risk Register", Type = "Application", Guid = "GUID-RISK-REG", Alias = "risk_reg" },
            new Module { Id = 2, Name = "Incident Management", Type = "Application", Guid = "GUID-INC-MGT", Alias = "inc_mgt" },
            new Module { Id = 3, Name = "Questionnaire A", Type = "Questionnaire", Guid = "GUID-QUEST-A", Alias = "quest_a" },
            new Module { Id = 4, Name = "SubForm B", Type = "SubForm", Guid = "GUID-SUB-B", Alias = "sub_b" }
        };
        
        return Task.FromResult(modules);
    }

    public Task<ArcherMetadata> CollectMetadataAsync(
        ArcherEnvironment environment, 
        string password,
        CollectionOptions options, 
        IProgress<(string Message, int Percentage)> progress)
    {
        var metadata = new ArcherMetadata
        {
            EnvironmentName = environment.DisplayName,
            ArcherVersion = "6.9.100.1000",
            CollectionDate = DateTime.Now
        };

        // If "Risk Register" is selected (Id=1), generate its mock data
        if (options.SelectedModuleIds.Contains(1) || options.IncludeModules)
        {
            progress.Report(($"Collecting metadata for Risk Register...", 10));
            
            // 1. Modules
            metadata.Modules.Add(new Module 
            { 
                Id = 1, 
                Name = "Risk Register", 
                Type = "Application", 
                Guid = "GUID-RISK-REG", 
                Alias = "risk_reg",
                UpdatedDate = DateTime.Now.AddDays(-10),
                UpdatedBy = "System Admin"
            });

            // 2. Fields
            if (options.IncludeFields)
            {
                // Field 1: Match in both (if consistent mock)
                // We'll vary slightly based on environment name to simulate differences
                bool isDev = environment.DisplayName.Contains("Dev", StringComparison.OrdinalIgnoreCase);
                
                metadata.Fields.Add(new Field 
                { 
                    Id = 101, 
                    Name = "Risk Title", 
                    Module = "Risk Register",
                    Level = "Risk Assessment",
                    TypeLabel = "Text",
                    Guid = "GUID-FLD-TITLE"
                });

                metadata.Fields.Add(new Field 
                { 
                    Id = 102, 
                    Name = "Risk Score", 
                    Module = "Risk Register",
                    Level = "Risk Assessment",
                    TypeLabel = "Numeric",
                    Guid = "GUID-FLD-SCORE",
                    IsCalculated = true,
                    // Difference: Formula might be different
                    Formula = isDev ? "(Impact * Likelihood) + 1" : "Impact * Likelihood" 
                });
                
                if (isDev)
                {
                    // Dev Only Field
                    metadata.Fields.Add(new Field 
                    { 
                        Id = 103, 
                        Name = "Dev Only Field", 
                        Module = "Risk Register",
                        Level = "Risk Assessment",
                        TypeLabel = "Text",
                        Guid = "GUID-FLD-DEV"
                    });
                }
                else
                {
                    // Prod Only Field
                    metadata.Fields.Add(new Field 
                    { 
                        Id = 104, 
                        Name = "Legacy Field", 
                        Module = "Risk Register",
                        Level = "Risk Assessment",
                        TypeLabel = "Text",
                        Guid = "GUID-FLD-PROD"
                    });
                }
            }

            // 3. Layouts
            if (options.IncludeLayouts)
            {
                metadata.Layouts.Add(new Layout 
                { 
                    Id = 201, 
                    Name = "Default Layout", 
                    Module = "Risk Register",
                    Level = "Risk Assessment",
                    LayoutName = "Default",
                    LayoutTab = "General",
                    LayoutSection = "Risk Details",
                    LayoutField = "Risk Title",
                    Guid = "GUID-LAY-1"
                });
            }
            
            // 4. DDE Rules
            if (options.IncludeDDERules)
            {
                metadata.DDERules.Add(new DDERule 
                { 
                    Id = 301, 
                    Name = "Hide Score if Inactive", 
                    Guid = "GUID-DDE-1",
                    IsActive = true
                });
            }
        }
        
        progress.Report(("Collection complete", 100));
        return Task.FromResult(metadata);
    }
}
