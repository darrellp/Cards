using Avalonia;
using Avalonia.Controls;

namespace SolitaireUI.Controls;

public partial class StatusBarView : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<StatusBarView, string>(nameof(Title), defaultValue: string.Empty);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public StatusBarView()
    {
        InitializeComponent();
    }
}
