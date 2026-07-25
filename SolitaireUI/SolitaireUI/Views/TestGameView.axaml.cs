using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SolitaireUI.ViewModels;
using System.ComponentModel;

namespace SolitaireUI.Views;

public partial class TestGameView : UserControl
{
    public TestGameView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Focus();
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is TestGameViewModel viewModel)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TestGameViewModel.IsGameOverDialogVisible)
            && sender is TestGameViewModel { IsGameOverDialogVisible: false })
        {
            Focus();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && DataContext is TestGameViewModel viewModel)
        {
            viewModel.ApplyAiMoveCommand.Execute(null);
            e.Handled = true;
        }
    }
}
