using System.Windows;
using ArcherComparisonTool.WPF.ViewModels;

namespace ArcherComparisonTool.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}