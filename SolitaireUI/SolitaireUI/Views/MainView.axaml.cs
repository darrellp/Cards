using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SolitaireUI.ViewModels;

namespace SolitaireUI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Focus();
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