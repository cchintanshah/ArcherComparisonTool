using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArcherComparisonTool.Core.Models;
using ArcherComparisonTool.Core.Services;
using Microsoft.Win32;
using Serilog;

namespace ArcherComparisonTool.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly EnvironmentStorage _environmentStorage;
    private readonly ExcelExporter _excelExporter;
    
    [ObservableProperty]
    private ObservableCollection<ArcherEnvironment> _environments = new();
    
    [ObservableProperty]
    private ArcherEnvironment? _sourceEnvironment;
    
    [ObservableProperty]
    private ArcherEnvironment? _targetEnvironment;
    
    [ObservableProperty]
    private ComparisonReport? _comparisonReport;
    
    [ObservableProperty]
    private CollectionOptions _collectionOptions = new();
    
    [ObservableProperty]
    private bool _isComparing;
    
    [ObservableProperty]
    private int _progressPercentage;
    
    [ObservableProperty]
    private string _progressMessage = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<ComparisonResult> _sourceOnlyResults = new();
    
    [ObservableProperty]
    private ObservableCollection<ComparisonResult> _targetOnlyResults = new();
    
    [ObservableProperty]
    private ObservableCollection<ComparisonResult> _mismatchResults = new();
    
    [ObservableProperty]
    private string _sourceOnlyHeader = "Not in Source";
    
    [ObservableProperty]
    private string _targetOnlyHeader = "Not in Target";
    
    [ObservableProperty]
    private string _mismatchHeader = "Mismatches";
    
    private readonly IMetadataService _metadataService;

    public MainViewModel()
    {
        _environmentStorage = new EnvironmentStorage();
        _excelExporter = new ExcelExporter();
        
        // Use Mock Service for testing as requested
        _metadataService = new MockMetadataService();
        // For production, use: _metadataService = new ArcherMetadataService(new ArcherApiClient());
        
        LoadEnvironmentsAsync();
    }
    
    private async void LoadEnvironmentsAsync()
    {
        var envs = await _environmentStorage.LoadEnvironmentsAsync();
        Environments = new ObservableCollection<ArcherEnvironment>(envs);
    }
    
    [RelayCommand]
    private void AddEnvironment()
    {
        var dialog = new Views.EnvironmentManagerDialog();
        var viewModel = new EnvironmentManagerViewModel();
        dialog.DataContext = viewModel;
        
        if (dialog.ShowDialog() == true)
        {
            Environments.Add(viewModel.Environment);
            SaveEnvironmentsAsync();
        }
    }
    
    [RelayCommand]
    private void EditEnvironment()
    {
        if (SourceEnvironment == null) return;
        
        var dialog = new Views.EnvironmentManagerDialog();
        var viewModel = new EnvironmentManagerViewModel(SourceEnvironment);
        dialog.DataContext = viewModel;
        
        if (dialog.ShowDialog() == true)
        {
            SaveEnvironmentsAsync();
        }
    }
    
    [RelayCommand]
    private void DeleteEnvironment()
    {
        if (SourceEnvironment == null) return;
        
        var result = MessageBox.Show(
            $"Are you sure you want to delete '{SourceEnvironment.DisplayName}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            Environments.Remove(SourceEnvironment);
            SaveEnvironmentsAsync();
        }
    }
    
    [RelayCommand]
    private async void ConfigureCollection()
    {
        if (SourceEnvironment == null)
        {
            MessageBox.Show("Please select a source environment first.", "No Environment Selected",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new Views.CollectionOptionsDialog();
        var viewModel = new CollectionOptionsViewModel(SourceEnvironment, CollectionOptions);
        dialog.DataContext = viewModel;
        
        if (dialog.ShowDialog() == true)
        {
            CollectionOptions = viewModel.Options;
        }
    }

    [RelayCommand]
    private async Task CompareAsync()
    {
        if (SourceEnvironment == null || TargetEnvironment == null)
        {
            MessageBox.Show("Please select both source and target environments.", "Missing Selection",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (SourceEnvironment.Id == TargetEnvironment.Id)
        {
            MessageBox.Show("Source and target environments must be different.", "Invalid Selection",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            IsComparing = true;
            ProgressPercentage = 0;
            
            // Clear previous results
            SourceOnlyResults.Clear();
            TargetOnlyResults.Clear();
            MismatchResults.Clear();
            
            // Set Dynamic Headers
            // "Not in Source" means present in Target but missing in Source.
            // "Not in Target" means present in Source but missing in Target.
            // Wait, usually "Source Only" means "Not in Target".
            // Let's clarify tabs based on user request: "not in source, not in target, mismatch"
            
            // Tab 1: "Not in Target" (Items present in Source only) -> Source Environment Name
            TargetOnlyHeader = $"Not in {TargetEnvironment.DisplayName}"; // "Not in UAT" (Source Only items)
            
            // Tab 2: "Not in Source" (Items present in Target only) -> Target Environment Name
            SourceOnlyHeader = $"Not in {SourceEnvironment.DisplayName}"; // "Not in DEV" (Target Only items)
            
            MismatchHeader = "Mismatches";
            
            // Get passwords (mock service ignores them but interface requires)
            var sourcePassword = GetPassword(SourceEnvironment) ?? "mock";
            var targetPassword = GetPassword(TargetEnvironment) ?? "mock";
            
            // Collect metadata from source using Service
            ProgressMessage = $"Collecting metadata from {SourceEnvironment.DisplayName}...";
            var progress = new Progress<(string Message, int Percentage)>(p => 
            {
                ProgressMessage = p.Message;
                ProgressPercentage = p.Percentage / 2; // Source is first 50%
            });
            
            var sourceMetadata = await _metadataService.CollectMetadataAsync(
                SourceEnvironment, sourcePassword, CollectionOptions, progress);
            
            // Collect metadata from target
            ProgressMessage = $"Collecting metadata from {TargetEnvironment.DisplayName}...";
            var targetProgress = new Progress<(string Message, int Percentage)>(p => 
            {
                ProgressMessage = p.Message;
                ProgressPercentage = 50 + (p.Percentage / 2); // Target is second 50%
            });
            
            var targetMetadata = await _metadataService.CollectMetadataAsync(
                TargetEnvironment, targetPassword, CollectionOptions, targetProgress);
            
            // Compare
            ProgressMessage = "Comparing environments...";
            var comparisonEngine = new ComparisonEngine();
            ComparisonReport = comparisonEngine.CompareEnvironments(sourceMetadata, targetMetadata, CollectionOptions);
            
            // Distribute results to tabs
            var allResults = ComparisonReport.GetAllResults();
            
            foreach (var result in allResults)
            {
                if (result.Status == ComparisonStatus.MissingInTarget)
                {
                    TargetOnlyResults.Add(result); // "Not in Target"
                }
                else if (result.Status == ComparisonStatus.MissingInSource)
                {
                    SourceOnlyResults.Add(result); // "Not in Source"
                }
                else if (result.Status == ComparisonStatus.Mismatch)
                {
                    MismatchResults.Add(result);
                }
            }
            
            ProgressPercentage = 100;
            ProgressMessage = $"Comparison complete!";
            
            MessageBox.Show($"Comparison completed successfully!\n\n" +
                            $"Not in {TargetEnvironment.DisplayName}: {TargetOnlyResults.Count}\n" +
                            $"Not in {SourceEnvironment.DisplayName}: {SourceOnlyResults.Count}\n" +
                            $"Mismatches: {MismatchResults.Count}",
                "Comparison Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Comparison failed");
            MessageBox.Show($"Comparison failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsComparing = false;
        }
    }
    
    [RelayCommand]
    private async Task ExportAsync()
    {
        if (ComparisonReport == null)
        {
            MessageBox.Show("No comparison results to export.", "No Results",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"Archer_Comparison_{SourceEnvironment?.DisplayName}_vs_{TargetEnvironment?.DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _excelExporter.ExportComparisonReportAsync(ComparisonReport, dialog.FileName);
                MessageBox.Show($"Report exported successfully to:\n{dialog.FileName}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Export failed");
                MessageBox.Show($"Export failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private async void SaveEnvironmentsAsync()
    {
        await _environmentStorage.SaveEnvironmentsAsync(Environments.ToList());
    }
    
    private string? GetPassword(ArcherEnvironment environment)
    {
        if (environment.EncryptedPassword != null)
        {
            try
            {
                return EnvironmentStorage.DecryptPassword(environment.EncryptedPassword);
            }
            catch
            {
                // Password decryption failed, prompt user
            }
        }
        
        // Prompt for password
        var passwordDialog = new Views.PasswordDialog(environment.DisplayName);
        if (passwordDialog.ShowDialog() == true)
        {
            return passwordDialog.Password;
        }
        
        return null;
    }
}
