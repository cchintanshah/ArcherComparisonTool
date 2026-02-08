using CommunityToolkit.Mvvm.ComponentModel;

namespace ArcherComparisonTool.WPF.Models;

public partial class ModuleSelectionItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;
    
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
}
