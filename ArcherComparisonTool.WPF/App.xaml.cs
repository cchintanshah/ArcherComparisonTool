using System.Windows;
using Serilog;

namespace ArcherComparisonTool.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Configure Serilog
        var logPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ArcherComparisonTool",
            "Logs",
            "log-.txt"
        );
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        // Force Light Theme with Blue Accent
        ModernWpf.ThemeManager.Current.ApplicationTheme = ModernWpf.ApplicationTheme.Light;
        ModernWpf.ThemeManager.Current.AccentColor = System.Windows.Media.Colors.DodgerBlue; // Fallback or explicit set
        
        Log.Information("Application started");
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application exiting");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
