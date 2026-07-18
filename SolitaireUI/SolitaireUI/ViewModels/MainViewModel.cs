using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cards;
using GenericSol.Games.TestGame;
using Klondike;
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

    private static TestGame _game = new();

    [ObservableProperty] public Stack _from = _game.StackFromName("From");
    [ObservableProperty] public Stack _to = _game.StackFromName("To");

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
        _game = new TestGame();
        SubscribeToGameEvents();

        From = _game.StackFromName("From");
        To = _game.StackFromName("To");

        IsGameOverDialogVisible = false;
    }
}