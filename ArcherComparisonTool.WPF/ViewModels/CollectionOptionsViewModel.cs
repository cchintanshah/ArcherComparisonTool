using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArcherComparisonTool.Core.Models;
using ArcherComparisonTool.Core.Services;
using ArcherComparisonTool.WPF.Models;

namespace ArcherComparisonTool.WPF.ViewModels;

public partial class CollectionOptionsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ModuleSelectionItem> _availableModules = new();
    
    [ObservableProperty]
    private string _moduleSearchText = string.Empty;
    
    [ObservableProperty]
    private bool _includeModules = true;
    
    [ObservableProperty]
    private bool _includeFields = true;
    
    [ObservableProperty]
    private bool _includeValuesLists = true;
    
    [ObservableProperty]
    private bool _includeLayouts = true;
    
    [ObservableProperty]
    private bool _includeDDERules = true;
    
    [ObservableProperty]
    private bool _includeDDEActions = true;
    
    [ObservableProperty]
    private bool _includeReports = false;
    
    [ObservableProperty]
    private bool _includeDashboards = false;
    
    [ObservableProperty]
    private bool _includeWorkspaces = false;
    
    [ObservableProperty]
    private bool _includeiViews = false;
    
    [ObservableProperty]
    private bool _includeRoles = false;
    
    [ObservableProperty]
    private bool _includeSecurityParameters = false;
    
    [ObservableProperty]
    private bool _includeNotifications = false;
    
    [ObservableProperty]
    private bool _includeDataFeeds = false;
    
    [ObservableProperty]
    private bool _includeSchedules = false;
    
    public CollectionOptions Options { get; private set; }
    
    public CollectionOptionsViewModel(ArcherEnvironment environment, CollectionOptions currentOptions)
    {
        Options = currentOptions;
        
        // Load current options
        IncludeModules = currentOptions.IncludeModules;
        IncludeFields = currentOptions.IncludeFields;
        IncludeValuesLists = currentOptions.IncludeValuesLists;
        IncludeLayouts = currentOptions.IncludeLayouts;
        IncludeDDERules = currentOptions.IncludeDDERules;
        IncludeDDEActions = currentOptions.IncludeDDEActions;
        IncludeReports = currentOptions.IncludeReports;
        IncludeDashboards = currentOptions.IncludeDashboards;
        IncludeWorkspaces = currentOptions.IncludeWorkspaces;
        IncludeiViews = currentOptions.IncludeiViews;
        IncludeRoles = currentOptions.IncludeRoles;
        IncludeSecurityParameters = currentOptions.IncludeSecurityParameters;
        IncludeNotifications = currentOptions.IncludeNotifications;
        IncludeDataFeeds = currentOptions.IncludeDataFeeds;
        IncludeSchedules = currentOptions.IncludeSchedules;
        
        // Load modules asynchronously
        LoadModulesAsync(environment);
    }
    
    private async void LoadModulesAsync(ArcherEnvironment environment)
    {
        try
        {
            // Get password
            string? password = null;
            if (environment.EncryptedPassword != null)
            {
                password = EnvironmentStorage.DecryptPassword(environment.EncryptedPassword);
            }
            
            if (password == null)
            {
                var passwordDialog = new Views.PasswordDialog(environment.DisplayName);
                if (passwordDialog.ShowDialog() == true)
                {
                    password = passwordDialog.Password;
                }
                else
                {
                    return;
                }
            }
            
            var client = new ArcherApiClient();
            await client.LoginAsync(environment, password);
            var modules = await client.GetModulesAsync();
            await client.LogoutAsync();
            
            AvailableModules = new ObservableCollection<ModuleSelectionItem>(
                modules.Select(m => new ModuleSelectionItem
                {
                    Id = m.Id,
                    Name = m.Name,
                    Type = m.Type,
                    IsSelected = Options.SelectedModuleIds.Contains(m.Id)
                })
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load modules: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    [RelayCommand]
    private void SelectAllModules()
    {
        foreach (var module in AvailableModules)
        {
            module.IsSelected = true;
        }
    }
    
    [RelayCommand]
    private void DeselectAllModules()
    {
        foreach (var module in AvailableModules)
        {
            module.IsSelected = false;
        }
    }
    
    [RelayCommand]
    private void SelectAllMetadata()
    {
        IncludeModules = true;
        IncludeFields = true;
        IncludeValuesLists = true;
        IncludeLayouts = true;
        IncludeDDERules = true;
        IncludeDDEActions = true;
        IncludeReports = true;
        IncludeDashboards = true;
        IncludeWorkspaces = true;
        IncludeiViews = true;
        IncludeRoles = true;
        IncludeSecurityParameters = true;
        IncludeNotifications = true;
        IncludeDataFeeds = true;
        IncludeSchedules = true;
    }
    
    [RelayCommand]
    private void DeselectAllMetadata()
    {
        IncludeModules = false;
        IncludeFields = false;
        IncludeValuesLists = false;
        IncludeLayouts = false;
        IncludeDDERules = false;
        IncludeDDEActions = false;
        IncludeReports = false;
        IncludeDashboards = false;
        IncludeWorkspaces = false;
        IncludeiViews = false;
        IncludeRoles = false;
        IncludeSecurityParameters = false;
        IncludeNotifications = false;
        IncludeDataFeeds = false;
        IncludeSchedules = false;
    }
    
    [RelayCommand]
    private void Save(Window window)
    {
        Options.SelectedModuleIds = AvailableModules.Where(m => m.IsSelected).Select(m => m.Id).ToList();
        Options.IncludeModules = IncludeModules;
        Options.IncludeFields = IncludeFields;
        Options.IncludeValuesLists = IncludeValuesLists;
        Options.IncludeLayouts = IncludeLayouts;
        Options.IncludeDDERules = IncludeDDERules;
        Options.IncludeDDEActions = IncludeDDEActions;
        Options.IncludeReports = IncludeReports;
        Options.IncludeDashboards = IncludeDashboards;
        Options.IncludeWorkspaces = IncludeWorkspaces;
        Options.IncludeiViews = IncludeiViews;
        Options.IncludeRoles = IncludeRoles;
        Options.IncludeSecurityParameters = IncludeSecurityParameters;
        Options.IncludeNotifications = IncludeNotifications;
        Options.IncludeDataFeeds = IncludeDataFeeds;
        Options.IncludeSchedules = IncludeSchedules;
        
        window.DialogResult = true;
        window.Close();
    }
    
    [RelayCommand]
    private void Cancel(Window window)
    {
        window.DialogResult = false;
        window.Close();
    }
}
