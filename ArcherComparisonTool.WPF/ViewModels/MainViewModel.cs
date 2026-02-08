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
    private ObservableCollection<ComparisonResult> _comparisonResults = new();
    
    public MainViewModel()
    {
        _environmentStorage = new EnvironmentStorage();
        _excelExporter = new ExcelExporter();
        
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
            ComparisonResults.Clear();
            
            // Get passwords
            var sourcePassword = GetPassword(SourceEnvironment);
            var targetPassword = GetPassword(TargetEnvironment);
            
            if (sourcePassword == null || targetPassword == null)
            {
                return;
            }
            
            // Collect metadata from source
            ProgressMessage = $"Collecting metadata from {SourceEnvironment.DisplayName}...";
            var sourceClient = new ArcherApiClient();
            var sourceCollector = new MetadataCollector(sourceClient);
            var progress = new Progress<string>(msg => ProgressMessage = msg);
            
            var sourceMetadata = await sourceCollector.CollectAllMetadataAsync(
                SourceEnvironment, sourcePassword, CollectionOptions, progress);
            
            ProgressPercentage = 50;
            
            // Collect metadata from target
            ProgressMessage = $"Collecting metadata from {TargetEnvironment.DisplayName}...";
            var targetClient = new ArcherApiClient();
            var targetCollector = new MetadataCollector(targetClient);
            
            var targetMetadata = await targetCollector.CollectAllMetadataAsync(
                TargetEnvironment, targetPassword, CollectionOptions, progress);
            
            ProgressPercentage = 75;
            
            // Compare
            ProgressMessage = "Comparing environments...";
            var comparisonEngine = new ComparisonEngine();
            ComparisonReport = comparisonEngine.CompareEnvironments(sourceMetadata, targetMetadata, CollectionOptions);
            
            // Update results
            var allResults = ComparisonReport.GetAllResults();
            ComparisonResults = new ObservableCollection<ComparisonResult>(allResults);
            
            ProgressPercentage = 100;
            ProgressMessage = $"Comparison complete! Found {allResults.Count} differences.";
            
            MessageBox.Show($"Comparison completed successfully!\n\nTotal differences: {allResults.Count}",
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
