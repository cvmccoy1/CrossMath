using CrossMath.Core;

namespace CrossMath.App.Services;

/// <summary>What the app remembers between runs.</summary>
public sealed record AppSettings(Difficulty Difficulty = Difficulty.Easy, WindowPlacement? Window = null);

/// <summary>
/// The window's normal (restored) bounds in device-independent pixels, and whether it was maximized.
/// Plain values so view models stay free of WPF types.
/// </summary>
public sealed record WindowPlacement(double Left, double Top, double Width, double Height, bool IsMaximized);
