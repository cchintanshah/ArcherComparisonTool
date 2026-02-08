using System.Security.Cryptography;
using System.Text.Json;
using ArcherComparisonTool.Core.Models;
using Serilog;

namespace ArcherComparisonTool.Core.Services;

public class EnvironmentStorage
{
    private readonly string _storageFilePath;
    
    public EnvironmentStorage()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ArcherComparisonTool"
        );
        
        Directory.CreateDirectory(appDataPath);
        _storageFilePath = Path.Combine(appDataPath, "environments.json");
    }
    
    public async Task SaveEnvironmentsAsync(List<ArcherEnvironment> environments)
    {
        try
        {
            var json = JsonSerializer.Serialize(environments, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_storageFilePath, json);
            Log.Information("Saved {Count} environments", environments.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save environments");
            throw;
        }
    }
    
    public async Task<List<ArcherEnvironment>> LoadEnvironmentsAsync()
    {
        try
        {
            if (!File.Exists(_storageFilePath))
            {
                return new List<ArcherEnvironment>();
            }
            
            var json = await File.ReadAllTextAsync(_storageFilePath);
            var environments = JsonSerializer.Deserialize<List<ArcherEnvironment>>(json) 
                ?? new List<ArcherEnvironment>();
            
            Log.Information("Loaded {Count} environments", environments.Count);
            return environments;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load environments");
            return new List<ArcherEnvironment>();
        }
    }
    
    public static byte[] EncryptPassword(string password)
    {
        try
        {
            var data = System.Text.Encoding.UTF8.GetBytes(password);
            return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to encrypt password");
            throw;
        }
    }
    
    public static string DecryptPassword(byte[] encryptedPassword)
    {
        try
        {
            var data = ProtectedData.Unprotect(encryptedPassword, null, DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(data);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to decrypt password");
            throw;
        }
    }
}
