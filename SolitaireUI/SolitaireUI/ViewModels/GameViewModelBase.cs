using Avalonia.Media;
using Cards;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenericSol;
using System;

namespace SolitaireUI.ViewModels;

/// <summary>
/// Base class for game view models that consolidates the win/lose ("game over") dialog
/// state and the subscription logic that drives it, as well as the drag/drop machinery
/// shared by every game view model that hosts draggable <see cref="Cards.Stack"/> piles.
/// Concrete game view models call
/// <see cref="SubscribeToGameEvents"/>/<see cref="UnsubscribeFromGameEvents"/> whenever the
/// active <see cref="IGame"/> instance is (re)created, e.g. in their constructor and in
/// their ResetGame command.
/// </summary>
public abstract partial class GameViewModelBase : ViewModelBase, IGameOverDialogViewModel, IDragDropViewModel
{
    [ObservableProperty] private bool _isGameOverDialogVisible;
    [ObservableProperty] private string _gameOverMessage = string.Empty;
    [ObservableProperty] private IBrush _gameOverBackground = Brushes.Transparent;

    // Drag state
    [ObservableProperty] private Stack? _dragSourceStack;
    [ObservableProperty] private string? _dragSourceName;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDragging))]
    private Stack? _tempDragStack;
    [ObservableProperty] private int _dragCardCount;
    [ObservableProperty] private Stack? _currentHoverStack;
    [ObservableProperty] private double _dragX;
    [ObservableProperty] private double _dragY;
    [ObservableProperty] private double _dragOffsetX;
    [ObservableProperty] private double _dragOffsetY;

    public abstract IGame Game { get; }

    [RelayCommand]
    private void ResetGame()
    {
        ResetGameCore();
        IsGameOverDialogVisible = false;
    }

    /// <summary>
    /// Backs the status bar's "New Game" button. Shares the same reset logic as
    /// <see cref="ResetGameCommand"/> (used by the game-over dialog's "Play Again" button).
    /// </summary>
    [RelayCommand]
    private void NewGame()
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

    public void HandleStackRightClick(Stack stack)
    {
        if (Game is GenericGame game)
        {
            game.OnRightClick(stack);
        }
    }


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
