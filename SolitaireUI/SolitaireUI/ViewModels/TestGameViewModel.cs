using Avalonia.Media;
using Avalonia.Media.Imaging;
using Cards;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenericSol;
using GenericSol.Games.TestGame;
using System;

namespace SolitaireUI.ViewModels;

public partial class TestGameViewModel : ViewModelBase, IDragDropViewModel
{
    private readonly MainWindowViewModel _mainWindowViewModel;
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
    private static IGame _game = _testGame;

    public IGame Game => _game;

    [ObservableProperty] private Stack? _from;
    [ObservableProperty] private Stack? _to;

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

    public TestGameViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _from = _testGame.StackFromName("From");
        _to = _testGame.StackFromName("To");
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
        _game = _testGame = new TestGame();
        SubscribeToGameEvents();

        From = _testGame.StackFromName("From");
        To = _testGame.StackFromName("To");

        IsGameOverDialogVisible = false;
    }

    [RelayCommand]
    private void BackToGameSelect()
    {
        _mainWindowViewModel.NavigateToGameSelect();
    }

    public void HandleStackRightClick(Stack stack)
    {
        if (_game is GenericGame game)
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
        TempDragStack = MixedStack.FromStack(splitStack, cardCount);

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

        if (hoverStack != null && hoverStack == DragSourceStack)
        {
            CurrentHoverStack = null;
            return;
        }

        if (hoverStack != null && _game is GenericGame game)
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

        if (CurrentHoverStack != null && _game is GenericGame game)
        {
            game.StackDrop(TempDragStack, DragSourceName, CurrentHoverStack, DragCardCount);
            game.OnStackSplit(DragSourceStack);
        }
        else
        {
            DragSourceStack.Merge(TempDragStack);
        }

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
}
