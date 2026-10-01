using System.Windows;
using System.Windows.Controls;
using CrossMath.App.ViewModels;
using CrossMath.Core;

namespace CrossMath.App.Converters;

/// <summary>Picks the board template for a cell based on its kind.</summary>
public sealed class CellTemplateSelector : DataTemplateSelector
{
    public DataTemplate? BlockedTemplate { get; set; }
    public DataTemplate? GivenTemplate { get; set; }
    public DataTemplate? BlankTemplate { get; set; }
    public DataTemplate? SymbolTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) => item switch
    {
        CellViewModel { Kind: CellKind.Number, IsBlank: true } => BlankTemplate,
        CellViewModel { Kind: CellKind.Number } => GivenTemplate,
        CellViewModel { Kind: CellKind.Operator or CellKind.Equals } => SymbolTemplate,
        _ => BlockedTemplate,
    };
}
