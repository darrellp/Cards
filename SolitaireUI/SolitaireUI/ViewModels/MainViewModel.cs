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

    [ObservableProperty] public Stack _from = _testGame.StackFromName("From");
    [ObservableProperty] public Stack _to = _testGame.StackFromName("To");

    [ObservableProperty] public Stack _stock = _klondikeGameModel.StackFromName("stock");
    [ObservableProperty] public Stack _waste = _klondikeGameModel.StackFromName("waste");
    [ObservableProperty] public Stack _fnd1 = _klondikeGameModel.StackFromName("fnd1");
    [ObservableProperty] public Stack _fnd2 = _klondikeGameModel.StackFromName("fnd2");
    [ObservableProperty] public Stack _fnd3 = _klondikeGameModel.StackFromName("fnd3");
    [ObservableProperty] public Stack _fnd4 = _klondikeGameModel.StackFromName("fnd4");
    [ObservableProperty] public Stack _tab1 = _klondikeGameModel.StackFromName("tab1");
    [ObservableProperty] public Stack _tab2 = _klondikeGameModel.StackFromName("tab2");
    [ObservableProperty] public Stack _tab3 = _klondikeGameModel.StackFromName("tab3");
    [ObservableProperty] public Stack _tab4 = _klondikeGameModel.StackFromName("tab4");
    [ObservableProperty] public Stack _tab5 = _klondikeGameModel.StackFromName("tab5");
    [ObservableProperty] public Stack _tab6 = _klondikeGameModel.StackFromName("tab6");
    [ObservableProperty] public Stack _tab7 = _klondikeGameModel.StackFromName("tab7");

    [ObservableProperty] public bool _isGameOverDialogVisible;
    [ObservableProperty] public string _gameOverMessage = string.Empty;
    [ObservableProperty] public IBrush _gameOverBackground = Brushes.Transparent;

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
}