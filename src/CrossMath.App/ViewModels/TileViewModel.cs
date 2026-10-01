using CommunityToolkit.Mvvm.ComponentModel;

namespace CrossMath.App.ViewModels;

/// <summary>A number tile the player moves between the pool and blank cells.</summary>
public partial class TileViewModel(int value) : ObservableObject
{
    public int Value { get; } = value;

    [ObservableProperty]
    private bool _isSelected;
}
