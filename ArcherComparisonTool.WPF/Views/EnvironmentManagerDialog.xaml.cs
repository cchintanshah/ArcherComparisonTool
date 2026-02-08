using System.Windows;

namespace ArcherComparisonTool.WPF.Views;

public partial class EnvironmentManagerDialog : Window
{
    public EnvironmentManagerDialog()
    {
        InitializeComponent();
        Loaded += (s, e) => PasswordBox.Password = ((ViewModels.EnvironmentManagerViewModel)DataContext).Password;
        PasswordBox.PasswordChanged += (s, e) => ((ViewModels.EnvironmentManagerViewModel)DataContext).Password = PasswordBox.Password;
    }
}
