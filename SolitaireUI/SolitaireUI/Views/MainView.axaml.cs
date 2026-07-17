using Avalonia.Controls;
using Avalonia.Input;
using SolitaireUI.ViewModels;

namespace SolitaireUI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && DataContext is MainViewModel viewModel)
        {
            viewModel.ApplyAiMoveCommand.Execute(null);
            e.Handled = true;
        }
    }
}