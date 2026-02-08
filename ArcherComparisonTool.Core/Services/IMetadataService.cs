using ArcherComparisonTool.Core.Models;
using ArcherComparisonTool.Core.Models.Metadata;

namespace ArcherComparisonTool.Core.Services;

public interface IMetadataService
{
    Task<ArcherMetadata> CollectMetadataAsync(ArcherEnvironment environment, string password, CollectionOptions options, IProgress<(string Message, int Percentage)> progress);
    Task<List<Module>> GetModulesAsync(ArcherEnvironment environment, string password);
}
