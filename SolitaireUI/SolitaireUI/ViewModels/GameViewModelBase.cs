using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenericSol;
using System;

namespace SolitaireUI.ViewModels;

/// <summary>
/// Base class for game view models that consolidates the win/lose ("game over") dialog
/// state and the subscription logic that drives it. Concrete game view models call
/// <see cref="SubscribeToGameEvents"/>/<see cref="UnsubscribeFromGameEvents"/> whenever the
/// active <see cref="IGame"/> instance is (re)created, e.g. in their constructor and in
/// their ResetGame command.
/// </summary>
public abstract partial class GameViewModelBase : ViewModelBase, IGameOverDialogViewModel
{
    [ObservableProperty] private bool _isGameOverDialogVisible;
    [ObservableProperty] private string _gameOverMessage = string.Empty;
    [ObservableProperty] private IBrush _gameOverBackground = Brushes.Transparent;

    [RelayCommand]
    private void ResetGame()
    {
        ResetGameCore();
        IsGameOverDialogVisible = false;
    }

    /// <summary>
    /// Each concrete game view model recreates its specific game model/stacks here and
    /// re-subscribes to the new instance's events (typically via
    /// <see cref="UnsubscribeFromGameEvents"/>/<see cref="SubscribeToGameEvents"/>).
    /// </summary>
    protected abstract void ResetGameCore();

    protected void SubscribeToGameEvents(IGame game)
    {
        game.GameState.Won += OnGameWon;
        game.GameState.Lost += OnGameLost;
    }

    protected void UnsubscribeFromGameEvents(IGame game)
    {
        game.GameState.Won -= OnGameWon;
        game.GameState.Lost -= OnGameLost;
    }

    private void OnGameWon(object? sender, EventArgs e)
    {
        GameOverMessage = "You Won!";
        GameOverBackground = Brushes.Green;
        IsGameOverDialogVisible = true;
    }

    private void OnGameLost(object? sender, EventArgs e)
    {
        GameOverMessage = "You Lost!";
        GameOverBackground = Brushes.Red;
        IsGameOverDialogVisible = true;
    }
}
