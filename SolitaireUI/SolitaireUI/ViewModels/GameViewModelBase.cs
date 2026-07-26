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

    public bool StartDrag(Stack sourceStack, int cardCount, double topLevelX, double topLevelY, double clickOffsetX, double clickOffsetY)
    {
        if (sourceStack == null || cardCount <= 0 || cardCount > sourceStack.Count)
        {
            return false;
        }

        DragSourceStack = sourceStack;
        DragSourceName = sourceStack.Name;
        DragCardCount = cardCount;
        var splitStack = sourceStack.Split(cardCount);
        // Render the drag ghost as a mixed stack with every card face up, regardless of how
        // the cards were represented in the source stack.
        TempDragStack = MixedStack.FromStack(splitStack, cardCount);

        // Note: unlike GenericGame.ApplyMove, we intentionally do NOT call game.OnStackSplit here.
        // When dragging with the mouse, the source stack should be left with 0 face-up cards
        // (rendered as a face-down peek) rather than auto-flipping the next card face up. The
        // flip only happens once the drag completes via StackDrop/CompleteDrag, or is reverted
        // on CancelDrag/invalid drop.
        CurrentHoverStack = null;
        DragOffsetX = clickOffsetX;
        DragOffsetY = clickOffsetY;
        DragX = topLevelX - clickOffsetX;
        DragY = topLevelY - clickOffsetY;
        return true;
    }

    public void UpdateDragHover(Stack? hoverStack, double topLevelX, double topLevelY)
    {
        DragX = topLevelX - DragOffsetX;
        DragY = topLevelY - DragOffsetY;

        if (TempDragStack == null || DragSourceName == null)
        {
            CurrentHoverStack = null;
            return;
        }

        // Don't hover over the source stack
        if (hoverStack != null && hoverStack == DragSourceStack)
        {
            CurrentHoverStack = null;
            return;
        }

        if (hoverStack != null && Game is GenericGame game)
        {
            if (game.IsMoveValid(TempDragStack, DragSourceName, hoverStack, DragCardCount))
            {
                CurrentHoverStack = hoverStack;
                return;
            }
        }

        CurrentHoverStack = null;
    }

    public void CompleteDrag()
    {
        if (TempDragStack == null || DragSourceStack == null || DragSourceName == null)
        {
            return;
        }

        if (CurrentHoverStack != null && Game is GenericGame game)
        {
            // Valid drop - commit the move and perform the same post-split bookkeeping
            // (e.g. flipping the next face-down card up) that an AI-driven move would.
            game.StackDrop(TempDragStack, DragSourceName, CurrentHoverStack, DragCardCount);
            game.OnStackSplit(DragSourceStack);
        }
        else
        {
            // Invalid drop - merge back to source; nothing was actually removed from the
            // source stack's perspective, so no flip bookkeeping is needed.
            DragSourceStack.Merge(TempDragStack);
        }

        // Clear drag state
        DragSourceStack = null;
        DragSourceName = null;
        TempDragStack = null;
        DragCardCount = 0;
        CurrentHoverStack = null;
    }

    public void CancelDrag()
    {
        if (TempDragStack != null && DragSourceStack != null)
        {
            DragSourceStack.Merge(TempDragStack);
        }

        DragSourceStack = null;
        DragSourceName = null;
        TempDragStack = null;
        DragCardCount = 0;
        CurrentHoverStack = null;
    }

    public bool IsDragging => TempDragStack != null;

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
