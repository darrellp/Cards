using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SolitaireUI.ViewModels;
using System.ComponentModel;

namespace SolitaireUI.Views;

public partial class GolfView : UserControl
{
    public GolfView()
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
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GolfViewModel.IsGameOverDialogVisible)
            && sender is GolfViewModel { IsGameOverDialogVisible: false })
        {
            Focus();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && DataContext is GolfViewModel viewModel)
        {
            viewModel.MakeAiMoveCommand.Execute(null);
            e.Handled = true;
        }
    }
}
