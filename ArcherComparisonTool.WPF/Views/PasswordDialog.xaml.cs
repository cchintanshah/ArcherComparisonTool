using System.Windows;

namespace ArcherComparisonTool.WPF.Views;

public partial class PasswordDialog : Window
{
    public string Password => PasswordBox.Password;
    
    public PasswordDialog(string environmentName)
    {
        InitializeComponent();
        MessageTextBlock.Text = $"Please enter the password for '{environmentName}':";
        PasswordBox.Focus();
    }
    
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
