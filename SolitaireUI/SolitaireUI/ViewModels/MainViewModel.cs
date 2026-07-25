using Avalonia.Media;
using Avalonia.Media.Imaging;
using Cards;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenericSol;
using GenericSol.Games.Klondike;
using GenericSol.Games.TestGame;
using System;

namespace SolitaireUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    static Bitmap[]? CardImages;

    static public Bitmap ImageFromCard(Card card)
    {
        if (CardImages is null)
        {
            CardImages = new Bitmap[52];
            var deck = Stack.SortedDeck();
            foreach (var cardCur in deck)
            {
                using (var stream = cardCur.ImageStream())
                    CardImages[cardCur.Index] = new Bitmap(stream);
            }
        }
        return CardImages[card.Index];
    }


    private static TestGame _testGame = new();
    private static KlondikeGame _klondikeGameModel = new();
    private static IGame _game = _klondikeGameModel;

    public IGame Game => _game;

    [ObservableProperty] private Stack _from = _testGame.StackFromName("From");
    [ObservableProperty] private Stack _to = _testGame.StackFromName("To");

    [ObservableProperty] private Stack _stock = _klondikeGameModel.StackFromName("stock");
    [ObservableProperty] private Stack _waste = _klondikeGameModel.StackFromName("waste");
    [ObservableProperty] private Stack _fnd1 = _klondikeGameModel.StackFromName("fnd1");
    [ObservableProperty] private Stack _fnd2 = _klondikeGameModel.StackFromName("fnd2");
    [ObservableProperty] private Stack _fnd3 = _klondikeGameModel.StackFromName("fnd3");
    [ObservableProperty] private Stack _fnd4 = _klondikeGameModel.StackFromName("fnd4");
    [ObservableProperty] private Stack _tab1 = _klondikeGameModel.StackFromName("tab1");
    [ObservableProperty] private Stack _tab2 = _klondikeGameModel.StackFromName("tab2");
    [ObservableProperty] private Stack _tab3 = _klondikeGameModel.StackFromName("tab3");
    [ObservableProperty] private Stack _tab4 = _klondikeGameModel.StackFromName("tab4");
    [ObservableProperty] private Stack _tab5 = _klondikeGameModel.StackFromName("tab5");
    [ObservableProperty] private Stack _tab6 = _klondikeGameModel.StackFromName("tab6");
    [ObservableProperty] private Stack _tab7 = _klondikeGameModel.StackFromName("tab7");

    [ObservableProperty] private bool _isGameOverDialogVisible;
    [ObservableProperty] private string _gameOverMessage = string.Empty;
    [ObservableProperty] private IBrush _gameOverBackground = Brushes.Transparent;

    // Drag state
    [ObservableProperty] private Stack? _dragSourceStack;
    [ObservableProperty] private string? _dragSourceName;
    [ObservableProperty] private Stack? _tempDragStack;
    [ObservableProperty] private int _dragCardCount;
    [ObservableProperty] private Stack? _currentHoverStack;
    [ObservableProperty] private double _dragX;
    [ObservableProperty] private double _dragY;

    public MainViewModel()
    {
        SubscribeToGameEvents();
    }

    private void SubscribeToGameEvents()
    {
        _game.GameState.Won += OnGameWon;
        _game.GameState.Lost += OnGameLost;
    }

    private void UnsubscribeFromGameEvents()
    {
        _game.GameState.Won -= OnGameWon;
        _game.GameState.Lost -= OnGameLost;
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

    [RelayCommand]
    private void ApplyAiMove()
    {
        var nextMove = _game.Ai.GetNextMove();
        if (nextMove is not null)
        {
            _game.ApplyMove(nextMove);
        }
    }

    [RelayCommand]
    private void ResetGame()
    {
        UnsubscribeFromGameEvents();
        _game = _klondikeGameModel = new KlondikeGame();
        SubscribeToGameEvents();

        Stock = _klondikeGameModel.StackFromName("stock");
        Waste = _klondikeGameModel.StackFromName("waste");
        Fnd1 = _klondikeGameModel.StackFromName("fnd1");
        Fnd2 = _klondikeGameModel.StackFromName("fnd2");
        Fnd3 = _klondikeGameModel.StackFromName("fnd3");
        Fnd4 = _klondikeGameModel.StackFromName("fnd4");
        Tab1 = _klondikeGameModel.StackFromName("tab1");
        Tab2 = _klondikeGameModel.StackFromName("tab2");
        Tab3 = _klondikeGameModel.StackFromName("tab3");
        Tab4 = _klondikeGameModel.StackFromName("tab4");
        Tab5 = _klondikeGameModel.StackFromName("tab5");
        Tab6 = _klondikeGameModel.StackFromName("tab6");
        Tab7 = _klondikeGameModel.StackFromName("tab7");

        IsGameOverDialogVisible = false;
    }

    public void HandleStackRightClick(Stack stack)
    {
        if (_game is GenericGame game)
        {
            game.OnRightClick(stack);
        }
    }

    public bool StartDrag(Stack sourceStack, int cardCount, double mouseX, double mouseY)
    {
        if (sourceStack == null || cardCount <= 0 || cardCount > sourceStack.Count)
        {
            return false;
        }

        _dragSourceStack = sourceStack;
        _dragSourceName = sourceStack.Name;
        _dragCardCount = cardCount;
        _tempDragStack = sourceStack.Split(cardCount);

        // Note: unlike GenericGame.ApplyMove, we intentionally do NOT call game.OnStackSplit here.
        // When dragging with the mouse, the source stack should be left with 0 face-up cards
        // (rendered as a face-down peek) rather than auto-flipping the next card face up. The
        // flip only happens once the drag completes via StackDrop/CompleteDrag, or is reverted
        // on CancelDrag/invalid drop.
        _currentHoverStack = null;
        _dragX = mouseX;
        _dragY = mouseY;
        return true;
    }

    public void UpdateDragHover(Stack? hoverStack, double mouseX, double mouseY)
    {
        _dragX = mouseX;
        _dragY = mouseY;

        if (_tempDragStack == null || _dragSourceName == null)
        {
            _currentHoverStack = null;
            return;
        }

        // Don't hover over the source stack
        if (hoverStack != null && hoverStack == _dragSourceStack)
        {
            _currentHoverStack = null;
            return;
        }

        if (hoverStack != null && _game is GenericGame game)
        {
            if (game.IsMoveValid(_tempDragStack, _dragSourceName, hoverStack, _dragCardCount))
            {
                _currentHoverStack = hoverStack;
                return;
            }
        }

        _currentHoverStack = null;
    }

    public void CompleteDrag()
    {
        if (_tempDragStack == null || _dragSourceStack == null || _dragSourceName == null)
        {
            return;
        }

        if (_currentHoverStack != null && _game is GenericGame game)
        {
            // Valid drop - commit the move and perform the same post-split bookkeeping
            // (e.g. flipping the next face-down card up) that an AI-driven move would.
            game.StackDrop(_tempDragStack, _dragSourceName, _currentHoverStack, _dragCardCount);
            game.OnStackSplit(_dragSourceStack);
        }
        else
        {
            // Invalid drop - merge back to source; nothing was actually removed from the
            // source stack's perspective, so no flip bookkeeping is needed.
            _dragSourceStack.Merge(_tempDragStack);
        }

        // Clear drag state
        _dragSourceStack = null;
        _dragSourceName = null;
        _tempDragStack = null;
        _dragCardCount = 0;
        _currentHoverStack = null;
    }

    public void CancelDrag()
    {
        if (_tempDragStack != null && _dragSourceStack != null)
        {
            _dragSourceStack.Merge(_tempDragStack);
        }

        _dragSourceStack = null;
        _dragSourceName = null;
        _tempDragStack = null;
        _dragCardCount = 0;
        _currentHoverStack = null;
    }

    public bool IsDragging => _tempDragStack != null;
}
