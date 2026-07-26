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

public partial class MainViewModel : GameViewModelBase
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

    public override IGame Game => _game;

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

    public MainViewModel()
    {
        SubscribeToGameEvents(_game);
    }

    [RelayCommand]
    private void ApplyAiMove()
    {
        // Don't let the AI mutate game state while the user has a card picked up mid-drag;
        // the source stack has already had cards split out for the drag ghost, so an AI move
        // touching that same stack right now would corrupt it (and could throw, leaving the
        // mouse pointer capture stuck so no further drags would work).
        if (IsDragging)
        {
            return;
        }

        var nextMove = _game.Ai.GetNextMove();
        if (nextMove is not null)
        {
            _game.ApplyMove(nextMove);
        }
    }

    protected override void ResetGameCore()
    {
        UnsubscribeFromGameEvents(_game);
        _game = _klondikeGameModel = new KlondikeGame();
        SubscribeToGameEvents(_game);

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
    }
}
