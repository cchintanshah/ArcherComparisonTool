using System.Net;
using System.Net.Http.Headers;
using System.Security;
using System.ServiceModel;
using System.Text;
using System.Text.Json;
using ArcherComparisonTool.Core.Models;
using ArcherComparisonTool.Core.Models.Metadata;
using Serilog;

namespace ArcherComparisonTool.Core.Services;

public class ArcherApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private string? _sessionToken;
    private string? _baseUrl;
    private string? _apiBaseUrl;
    
    public ArcherApiClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
    }
    
    public async Task<string> LoginAsync(ArcherEnvironment environment, string password)
    {
        try
        {
            _baseUrl = environment.Url.TrimEnd('/') + "/";
            
            // Create SOAP envelope for authentication
            var soapEnvelope = CreateLoginSoapEnvelope(
                environment.Username,
                environment.InstanceName,
                password,
                environment.UserDomain
            );
            
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}ws/general.asmx", content);
            response.EnsureSuccessStatusCode();
            
            var responseBody = await response.Content.ReadAsStringAsync();
            
            // Extract session token from SOAP response
            _sessionToken = ExtractSessionToken(responseBody);
            
            if (string.IsNullOrEmpty(_sessionToken))
            {
                throw new Exception("Failed to extract session token from response");
            }
            
            Log.Information("Successfully logged in to Archer instance: {Instance}", environment.InstanceName);
            
            // Determine API base URL based on version
            var version = await GetVersionAsync();
            _apiBaseUrl = Version.Parse(version) >= new Version(6, 6) ? "/platformapi" : "/api";
            
            return _sessionToken;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Login failed for environment: {Environment}", environment.DisplayName);
            throw;
        }
    }
    
    private string CreateLoginSoapEnvelope(string username, string instanceName, string password, string? userDomain)
    {
        var methodName = string.IsNullOrEmpty(userDomain) 
            ? "CreateUserSessionFromInstance" 
            : "CreateDomainUserSessionFromInstance";
        
        var parameters = string.IsNullOrEmpty(userDomain)
            ? $"<userName>{SecurityElement.Escape(username)}</userName>" +
              $"<instanceName>{SecurityElement.Escape(instanceName)}</instanceName>" +
              $"<password>{SecurityElement.Escape(password)}</password>"
            : $"<userName>{SecurityElement.Escape(username)}</userName>" +
              $"<instanceName>{SecurityElement.Escape(instanceName)}</instanceName>" +
              $"<password>{SecurityElement.Escape(password)}</password>" +
              $"<usersDomain>{SecurityElement.Escape(userDomain)}</usersDomain>";
        
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <soap:Body>
    <{methodName} xmlns=""http://archer-tech.com/webservices/"">
      {parameters}
    </{methodName}>
  </soap:Body>
</soap:Envelope>";
    }
    
    private string ExtractSessionToken(string soapResponse)
    {
        // Simple XML parsing to extract session token
        var startTag = "Result>";
        var endTag = "</";
        
        var startIndex = soapResponse.IndexOf(startTag);
        if (startIndex == -1) return string.Empty;
        
        startIndex += startTag.Length;
        var endIndex = soapResponse.IndexOf(endTag, startIndex);
        
        if (endIndex == -1) return string.Empty;
        
        return soapResponse.Substring(startIndex, endIndex - startIndex).Trim();
    }
    
    public async Task<string> GetVersionAsync()
    {
        try
        {
            var url = $"{_baseUrl}api/core/system/applicationinfo/version";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Archer session-id={_sessionToken}");
            request.Headers.Add("X-Http-Method-Override", "GET");
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                // Try platformapi
                url = $"{_baseUrl}platformapi/core/system/applicationinfo/version";
                request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Archer session-id={_sessionToken}");
                request.Headers.Add("X-Http-Method-Override", "GET");
                response = await _httpClient.SendAsync(request);
            }
            
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            
            return doc.RootElement.GetProperty("RequestedObject").GetProperty("Version").GetString() ?? "Unknown";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get Archer version");
            return "Unknown";
        }
    }
    
    public async Task<List<Models.Metadata.Module>> GetModulesAsync()
    {
        try
        {
            // Only retrieve Application, Questionnaire, and SubForm module types
            var url = $"{_baseUrl}api/V2/internal/ManageModules?$filter=(Type eq 'Application' or Type eq 'SubForm' or Type eq 'Questionnaire')";
            var json = await GetAsync(url);
            var doc = JsonDocument.Parse(json);
            
            var modules = new List<Models.Metadata.Module>();
            if (doc.RootElement.TryGetProperty("value", out var valueArray))
            {
                modules = JsonSerializer.Deserialize<List<Models.Metadata.Module>>(valueArray.GetRawText()) ?? new List<Models.Metadata.Module>();
            }
            
            // Additional client-side filtering to ensure only desired types
            modules = modules.Where(m => 
                m.Type == "Application" || 
                m.Type == "SubForm" || 
                m.Type == "Questionnaire"
            ).ToList();
            
            Log.Information("Retrieved {Count} modules (Application, Questionnaire, SubForm only)", modules.Count);
            return modules;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get modules");
            return new List<Models.Metadata.Module>();
        }
    }
    
    public async Task<List<Field>> GetFieldsAsync(int levelId)
    {
        try
        {
            var url = $"{_baseUrl}api/V2/internal/ManageLevels({levelId})/FieldRows";
            var json = await GetAsync(url);
            var fields = JsonSerializer.Deserialize<List<Field>>(json) ?? new List<Field>();
            return fields;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get fields for level {LevelId}", levelId);
            return new List<Field>();
        }
    }
    
    public async Task<List<Report>> GetReportsAsync()
    {
        try
        {
            var url = $"{_baseUrl}api/V2/internal/MasterReports?$select=SolutionName,ModuleName,ReportTypeDisplayColumnString,Id,ReportId,Name,LastUpdatedBy,LastUpdatedDate,Description&$count=true&$orderby=SolutionName,ModuleName,Name";
            var json = await GetAsync(url);
            var doc = JsonDocument.Parse(json);
            
            var reports = new List<Report>();
            if (doc.RootElement.TryGetProperty("value", out var valueArray))
            {
                reports = JsonSerializer.Deserialize<List<Report>>(valueArray.GetRawText()) ?? new List<Report>();
            }
            
            Log.Information("Retrieved {Count} reports", reports.Count);
            return reports;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get reports");
            return new List<Report>();
        }
    }
    
    public async Task<List<Dashboard>> GetDashboardsAsync()
    {
        try
        {
            var url = $"{_baseUrl}api/V2/internal/Dashboards";
            var json = await GetAsync(url);
            var doc = JsonDocument.Parse(json);
            
            var dashboards = new List<Dashboard>();
            if (doc.RootElement.TryGetProperty("value", out var valueArray))
            {
                dashboards = JsonSerializer.Deserialize<List<Dashboard>>(valueArray.GetRawText()) ?? new List<Dashboard>();
            }
            
            Log.Information("Retrieved {Count} dashboards", dashboards.Count);
            return dashboards;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get dashboards");
            return new List<Dashboard>();
        }
    }
    
    public async Task LogoutAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_sessionToken)) return;
            
            var url = $"{_baseUrl}api/core/security/logout";
            var body = JsonSerializer.Serialize(new { Value = _sessionToken });
            await PostAsync(url, body);
            
            Log.Information("Successfully logged out");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Logout failed");
        }
    }
    
    private async Task<string> GetAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Archer session-id={_sessionToken}");
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }
    
    private async Task<string> PostAsync(string url, string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Archer session-id={_sessionToken}");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }
    
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
