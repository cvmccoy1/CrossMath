using CommunityToolkit.Mvvm.ComponentModel;
using CrossMath.Core;

namespace CrossMath.App.ViewModels;

public enum CellState { Normal, Correct, Wrong, Hinted }

/// <summary>One square of the board: blocked, a given number, a blank to fill, an operator or "=".</summary>
public partial class CellViewModel : ObservableObject
{
    private readonly int? _givenValue;

    public CellViewModel(Pos pos, CellKind kind, bool isBlank, string text = "", int? givenValue = null)
    {
        Pos = pos;
        Kind = kind;
        IsBlank = isBlank;
        Text = text;
        _givenValue = givenValue;
    }

    public Pos Pos { get; }
    public CellKind Kind { get; }
    public bool IsBlank { get; }

    /// <summary>Fixed text for given numbers, operators and "=".</summary>
    public string Text { get; }

    /// <summary>Crossword-style number shown in the corner of a cell where an equation starts.</summary>
    public int? ClueNumber { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText), nameof(HasTile), nameof(CanDrag), nameof(Value))]
    private TileViewModel? _tile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDrag))]
    private bool _isLocked;

    [ObservableProperty]
    private CellState _state;

    [ObservableProperty]
    private bool _isSelected;

    public string DisplayText => IsBlank ? Tile?.Value.ToString() ?? string.Empty : Text;

    public bool HasTile => Tile is not null;

    /// <summary>A placed, non-hinted tile can be picked up again.</summary>
    public bool CanDrag => IsBlank && Tile is not null && !IsLocked;

    /// <summary>The number currently shown in this cell, if any.</summary>
    public int? Value => IsBlank ? Tile?.Value : _givenValue;
}
