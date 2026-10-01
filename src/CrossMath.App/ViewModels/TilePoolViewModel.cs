using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrossMath.App.ViewModels;

/// <summary>The unplaced tiles, kept sorted by value.</summary>
public partial class TilePoolViewModel : ObservableObject
{
    public ObservableCollection<TileViewModel> Tiles { get; } = [];

    public bool Contains(TileViewModel tile) => Tiles.Contains(tile);

    public void Add(TileViewModel tile)
    {
        int index = 0;
        while (index < Tiles.Count && Tiles[index].Value <= tile.Value) index++;
        tile.IsSelected = false;
        Tiles.Insert(index, tile);
    }

    public bool Remove(TileViewModel tile) => Tiles.Remove(tile);

    public void Clear() => Tiles.Clear();
}
