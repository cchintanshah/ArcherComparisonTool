using System.Security;

namespace ArcherComparisonTool.Core.Models;

public class ArcherEnvironment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string UserDomain { get; set; } = string.Empty;
    
    // Encrypted password storage
    public byte[]? EncryptedPassword { get; set; }
    
    public override string ToString() => DisplayName;
}
