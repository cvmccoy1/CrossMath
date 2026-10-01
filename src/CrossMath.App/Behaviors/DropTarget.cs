using System.Windows;
using System.Windows.Input;
using CrossMath.App.ViewModels;

namespace CrossMath.App.Behaviors;

/// <summary>
/// Attached behavior that accepts items dragged by <see cref="DragSource"/> and runs
/// <see cref="CommandProperty"/> with a <see cref="DropRequest"/>(dragged item, this element's DataContext).
/// The command's CanExecute decides whether the drop is allowed.
/// </summary>
public static class DropTarget
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command", typeof(ICommand), typeof(DropTarget), new PropertyMetadata(null, OnCommandChanged));

    public static ICommand? GetCommand(DependencyObject d) => (ICommand?)d.GetValue(CommandProperty);
    public static void SetCommand(DependencyObject d, ICommand? value) => d.SetValue(CommandProperty, value);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        element.DragEnter -= OnDragOver;
        element.DragOver -= OnDragOver;
        element.Drop -= OnDrop;

        element.AllowDrop = e.NewValue is not null;
        if (e.NewValue is null) return;

        element.DragEnter += OnDragOver;
        element.DragOver += OnDragOver;
        element.Drop += OnDrop;
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        var (command, request) = Resolve(sender, e);
        e.Effects = command is not null && command.CanExecute(request) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        var (command, request) = Resolve(sender, e);
        if (command is not null && command.CanExecute(request)) command.Execute(request);
        e.Handled = true;
    }

    private static (ICommand? Command, DropRequest? Request) Resolve(object sender, DragEventArgs e)
    {
        var element = (FrameworkElement)sender;
        if (!e.Data.GetDataPresent(DragSource.DataFormat) || element.DataContext is null)
            return (null, null);

        var source = e.Data.GetData(DragSource.DataFormat);
        return source is null ? (null, null) : (GetCommand(element), new DropRequest(source, element.DataContext));
    }
}
