using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SolitaireUI.ViewModels;
using System.ComponentModel;

namespace SolitaireUI.Views;

public partial class KlondikeView : UserControl
{
    public KlondikeView()
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
        if (DataContext is KlondikeViewModel viewModel)
        {
            //viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            //viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(KlondikeViewModel.IsGameOverDialogVisible)
            && sender is KlondikeViewModel { IsGameOverDialogVisible: false })
        {
            Focus();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && DataContext is KlondikeViewModel viewModel)
        {
            viewModel.ApplyAiMoveCommand.Execute(null);
            e.Handled = true;
        }
    }
}
