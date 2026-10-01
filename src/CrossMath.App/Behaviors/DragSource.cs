using System.Windows;
using System.Windows.Input;

namespace CrossMath.App.Behaviors;

/// <summary>
/// Attached behavior that makes an element draggable (payload: its DataContext) and turns a press
/// without a drag into <see cref="ClickCommandProperty"/>. Right-click runs <see cref="RightClickCommandProperty"/>.
/// Both commands receive the element's DataContext as their parameter.
/// </summary>
public static class DragSource
{
    public const string DataFormat = "CrossMath.DragItem";

    public static readonly DependencyProperty CanDragProperty = DependencyProperty.RegisterAttached(
        "CanDrag", typeof(bool), typeof(DragSource), new PropertyMetadata(false, OnAttach));

    public static readonly DependencyProperty ClickCommandProperty = DependencyProperty.RegisterAttached(
        "ClickCommand", typeof(ICommand), typeof(DragSource), new PropertyMetadata(null, OnAttach));

    public static readonly DependencyProperty RightClickCommandProperty = DependencyProperty.RegisterAttached(
        "RightClickCommand", typeof(ICommand), typeof(DragSource), new PropertyMetadata(null, OnAttach));

    private static readonly DependencyProperty IsHookedProperty = DependencyProperty.RegisterAttached(
        "IsHooked", typeof(bool), typeof(DragSource));

    private static readonly DependencyProperty PressPointProperty = DependencyProperty.RegisterAttached(
        "PressPoint", typeof(Point?), typeof(DragSource));

    public static bool GetCanDrag(DependencyObject d) => (bool)d.GetValue(CanDragProperty);
    public static void SetCanDrag(DependencyObject d, bool value) => d.SetValue(CanDragProperty, value);

    public static ICommand? GetClickCommand(DependencyObject d) => (ICommand?)d.GetValue(ClickCommandProperty);
    public static void SetClickCommand(DependencyObject d, ICommand? value) => d.SetValue(ClickCommandProperty, value);

    public static ICommand? GetRightClickCommand(DependencyObject d) => (ICommand?)d.GetValue(RightClickCommandProperty);
    public static void SetRightClickCommand(DependencyObject d, ICommand? value) => d.SetValue(RightClickCommandProperty, value);

    private static void OnAttach(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element || (bool)element.GetValue(IsHookedProperty)) return;

        element.SetValue(IsHookedProperty, true);
        element.PreviewMouseLeftButtonDown += OnLeftDown;
        element.PreviewMouseMove += OnMouseMove;
        element.PreviewMouseLeftButtonUp += OnLeftUp;
        element.MouseRightButtonUp += OnRightUp;
    }

    private static void OnLeftDown(object sender, MouseButtonEventArgs e)
    {
        var element = (UIElement)sender;
        element.SetValue(PressPointProperty, e.GetPosition(element));
    }

    private static void OnMouseMove(object sender, MouseEventArgs e)
    {
        var element = (UIElement)sender;
        if (element.GetValue(PressPointProperty) is not Point start || e.LeftButton != MouseButtonState.Pressed)
            return;

        var delta = e.GetPosition(element) - start;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        element.ClearValue(PressPointProperty);
        if (GetCanDrag(element) && element is FrameworkElement { DataContext: { } payload })
            DragDrop.DoDragDrop(element, new DataObject(DataFormat, payload), DragDropEffects.Move);
    }

    private static void OnLeftUp(object sender, MouseButtonEventArgs e)
    {
        var element = (UIElement)sender;
        if (element.GetValue(PressPointProperty) is not Point) return;

        element.ClearValue(PressPointProperty);
        Execute(GetClickCommand(element), element);
    }

    private static void OnRightUp(object sender, MouseButtonEventArgs e)
    {
        var element = (UIElement)sender;
        if (Execute(GetRightClickCommand(element), element)) e.Handled = true;
    }

    private static bool Execute(ICommand? command, UIElement element)
    {
        object? parameter = (element as FrameworkElement)?.DataContext;
        if (command is null || !command.CanExecute(parameter)) return false;
        command.Execute(parameter);
        return true;
    }
}
