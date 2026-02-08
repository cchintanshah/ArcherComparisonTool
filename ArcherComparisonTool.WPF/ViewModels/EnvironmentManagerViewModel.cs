using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArcherComparisonTool.Core.Models;
using ArcherComparisonTool.Core.Services;

namespace ArcherComparisonTool.WPF.ViewModels;

public partial class EnvironmentManagerViewModel : ObservableObject
{
    [ObservableProperty]
    private string _displayName = string.Empty;
    
    [ObservableProperty]
    private string _url = string.Empty;
    
    [ObservableProperty]
    private string _instanceName = string.Empty;
    
    [ObservableProperty]
    private string _username = string.Empty;
    
    [ObservableProperty]
    private string _password = string.Empty;
    
    [ObservableProperty]
    private string _userDomain = string.Empty;
    
    public ArcherEnvironment Environment { get; private set; }
    
    public EnvironmentManagerViewModel()
    {
        Environment = new ArcherEnvironment();
    }
    
    public EnvironmentManagerViewModel(ArcherEnvironment environment)
    {
        Environment = environment;
        DisplayName = environment.DisplayName;
        Url = environment.Url;
        InstanceName = environment.InstanceName;
        Username = environment.Username;
        UserDomain = environment.UserDomain;
        
        // Decrypt password if available
        if (environment.EncryptedPassword != null)
        {
            try
            {
                Password = EnvironmentStorage.DecryptPassword(environment.EncryptedPassword);
            }
            catch
            {
                // Password decryption failed
            }
        }
    }
    
    [RelayCommand]
    private void Save(Window window)
    {
        if (string.IsNullOrWhiteSpace(DisplayName) || 
            string.IsNullOrWhiteSpace(Url) ||
            string.IsNullOrWhiteSpace(InstanceName) ||
            string.IsNullOrWhiteSpace(Username))
        {
            MessageBox.Show("Please fill in all required fields.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        Environment.DisplayName = DisplayName;
        Environment.Url = Url;
        Environment.InstanceName = InstanceName;
        Environment.Username = Username;
        Environment.UserDomain = UserDomain;
        
        // Encrypt and save password
        if (!string.IsNullOrEmpty(Password))
        {
            Environment.EncryptedPassword = EnvironmentStorage.EncryptPassword(Password);
        }
        
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
