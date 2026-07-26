using Avalonia;
using Avalonia.Controls;

namespace SolitaireUI.Controls;

public partial class DragGhostCanvas : UserControl
{
    public static readonly StyledProperty<double> CardWidthProperty =
        AvaloniaProperty.Register<DragGhostCanvas, double>(nameof(CardWidth), defaultValue: 71.0);

    public static readonly StyledProperty<double> CardHeightProperty =
        AvaloniaProperty.Register<DragGhostCanvas, double>(nameof(CardHeight), defaultValue: 96.0);

    public static readonly StyledProperty<double> OverlapDistanceProperty =
        AvaloniaProperty.Register<DragGhostCanvas, double>(nameof(OverlapDistance), defaultValue: 20.0);

    public double CardWidth
    {
        get => GetValue(CardWidthProperty);
        set => SetValue(CardWidthProperty, value);
    }

    public double CardHeight
    {
        get => GetValue(CardHeightProperty);
        set => SetValue(CardHeightProperty, value);
    }

    public double OverlapDistance
    {
        get => GetValue(OverlapDistanceProperty);
        set => SetValue(OverlapDistanceProperty, value);
    }

    public DragGhostCanvas()
    {
        InitializeComponent();
    }
}
