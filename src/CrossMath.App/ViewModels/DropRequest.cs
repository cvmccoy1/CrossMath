namespace CrossMath.App.ViewModels;

/// <summary>
/// A request to move a tile. <see cref="Source"/> is a pool <see cref="TileViewModel"/> or a filled
/// <see cref="CellViewModel"/>; <see cref="Target"/> is a <see cref="CellViewModel"/> or the
/// <see cref="TilePoolViewModel"/> (or a tile inside it).
/// </summary>
public sealed record DropRequest(object Source, object Target);
