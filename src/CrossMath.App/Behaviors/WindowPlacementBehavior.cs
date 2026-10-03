using System.ComponentModel;
using System.Windows;
using CrossMath.App.Services;

namespace CrossMath.App.Behaviors;

/// <summary>
/// Attached behavior that restores a window's size, position and maximized state from
/// <see cref="PlacementProperty"/> before it is first shown, and writes them back (two-way) when it closes.
/// A saved position that is no longer on screen is ignored, so the window keeps its startup location.
/// </summary>
public static class WindowPlacementBehavior
{
    // How much of the window (in each direction) must be on screen for a saved position to be used.
    private const double MinVisible = 100;

    public static readonly DependencyProperty PlacementProperty = DependencyProperty.RegisterAttached(
        "Placement", typeof(WindowPlacement), typeof(WindowPlacementBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnPlacementChanged, HookClosing));

    public static WindowPlacement? GetPlacement(DependencyObject d) => (WindowPlacement?)d.GetValue(PlacementProperty);
    public static void SetPlacement(DependencyObject d, WindowPlacement? value) => d.SetValue(PlacementProperty, value);

    // Coercion runs whenever the binding supplies a value, even null on a first run (when the changed
    // callback doesn't fire), so this is where the window gets hooked up to save on close.
    private static object? HookClosing(DependencyObject d, object? value)
    {
        if (d is Window window)
        {
            window.Closing -= OnClosing;
            window.Closing += OnClosing;
        }
        return value;
    }

    private static void OnPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Restore only before the window first appears; later changes are this behavior writing back.
        if (d is Window { IsLoaded: false } window && e.NewValue is WindowPlacement placement) Apply(window, placement);
    }

    private static void Apply(Window window, WindowPlacement placement)
    {
        window.Width = Math.Max(placement.Width, window.MinWidth);
        window.Height = Math.Max(placement.Height, window.MinHeight);

        if (IsOnScreen(placement))
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = placement.Left;
            window.Top = placement.Top;
        }

        if (placement.IsMaximized) window.WindowState = WindowState.Maximized;
    }

    private static bool IsOnScreen(WindowPlacement p)
    {
        double left = SystemParameters.VirtualScreenLeft, top = SystemParameters.VirtualScreenTop;
        double right = left + SystemParameters.VirtualScreenWidth, bottom = top + SystemParameters.VirtualScreenHeight;
        double visibleWidth = Math.Min(p.Left + p.Width, right) - Math.Max(p.Left, left);
        double visibleHeight = Math.Min(p.Top + p.Height, bottom) - Math.Max(p.Top, top);
        return visibleWidth >= MinVisible && visibleHeight >= MinVisible && p.Top >= top; // title bar reachable
    }

    private static void OnClosing(object? sender, CancelEventArgs e)
    {
        var window = (Window)sender!;
        // RestoreBounds holds the normal-state bounds while the window is maximized or minimized.
        var bounds = window.WindowState == WindowState.Normal
            ? new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight)
            : window.RestoreBounds;
        if (bounds.IsEmpty) return;

        SetPlacement(window, new WindowPlacement(bounds.Left, bounds.Top, bounds.Width, bounds.Height,
            window.WindowState == WindowState.Maximized));
    }
}
