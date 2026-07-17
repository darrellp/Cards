using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cards;
using GenericSol.Games.TestGame;
using Klondike;

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
    // [ObservableProperty] public Bitmap _heartCardImage = ImageFromCard(Card.CardFromString("KH"));
    [ObservableProperty] public Stack _from = _game.StackFromName("From");
    [ObservableProperty] public Stack _to = _game.StackFromName("To");

    [RelayCommand]
    private void ApplyAiMove()
    {
        var nextMove = _game.Ai.GetNextMove();
        if (nextMove is not null)
        {
            _game.ApplyMove(nextMove);
        }
    }
}