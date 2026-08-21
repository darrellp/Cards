using Cards;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenericSol;
using GenericSol.Games.PyramidGame;
using System;

namespace SolitaireUI.ViewModels;

public partial class PyramidViewModel : GameViewModelBase, IStatusBarViewModel
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    private static PyramidGame _pyramidGame = new();
    private static IGame _game = _pyramidGame;

    public override IGame Game => _game;

    [ObservableProperty] private Stack _stock = _pyramidGame.StackFromName("stock");
    [ObservableProperty] private Stack _waste = _pyramidGame.StackFromName("waste");
    [ObservableProperty] private Stack _discards = _pyramidGame.StackFromName("discards");

    [ObservableProperty] private Stack[] _pyramid = _pyramidGame.PyramidStacks();

    // Layout constants for arranging the 28 pyramid stacks into a 7-row triangle, cascading
    // downward (like a Spider tableau) so each row overlaps the row above it vertically, while
    // cards within the same row sit side by side without any horizontal overlap.
    public const double PyramidCardWidth = 120;
    public const double PyramidCardHeight = 160;
    private const double HorizontalGap = 10;
    private const double VerticalStep = 50;
    private const int RowCount = 7;

    public static double PyramidCanvasWidth => RowCount * PyramidCardWidth + (RowCount - 1) * HorizontalGap;
    public static double PyramidCanvasHeight => (RowCount - 1) * VerticalStep + PyramidCardHeight;

    public double[] PyramidLeft { get; } = new double[28];
    public double[] PyramidTop { get; } = new double[28];

    public PyramidViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _stock = _pyramidGame.StackFromName("stock");
        _waste = _pyramidGame.StackFromName("waste");
        _discards = _pyramidGame.StackFromName("discards");
        _pyramid = _pyramidGame.PyramidStacks();
        ComputePyramidLayout();
        SubscribeToGameEvents(_game);
    }

    private void ComputePyramidLayout()
    {
        var centerX = PyramidCanvasWidth / 2;

        for (var row = 0; row < RowCount; row++)
        {
            var cardsInRow = row + 1;
            var rowWidth = cardsInRow * PyramidCardWidth + (cardsInRow - 1) * HorizontalGap;
            var rowLeft = centerX - rowWidth / 2;
            for (var col = 0; col <= row; col++)
            {
                var index = row * (row + 1) / 2 + col;
                PyramidLeft[index] = rowLeft + col * (PyramidCardWidth + HorizontalGap);
                PyramidTop[index] = row * VerticalStep;
            }
        }
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

    protected override void ResetGameCore()
    {
        UnsubscribeFromGameEvents(_game);
        _game = _pyramidGame = new PyramidGame();
        _mainWindowViewModel.ApplyStoredGameOptions(_game);
        SubscribeToGameEvents(_game);

        Stock = _pyramidGame.StackFromName("stock");
        Waste = _pyramidGame.StackFromName("waste");
        Discards = _pyramidGame.StackFromName("discards");
        Pyramid = _pyramidGame.PyramidStacks();
    }

    [RelayCommand]
    private void BackToGameSelect()
    {
        _mainWindowViewModel.NavigateToGameSelect();
    }

    [RelayCommand]
    private void CardBackSelect()
    {
        _mainWindowViewModel.NavigateToCardBackSelect();
    }

    [RelayCommand]
    private void Undo()
    {
        _game.Undo();
    }

    [RelayCommand]
    private void Info()
    {
        _mainWindowViewModel.NavigateToGameInfo(_game);
    }
}
