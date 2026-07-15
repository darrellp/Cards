using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Cards;

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

    // [ObservableProperty] public Bitmap _heartCardImage = ImageFromCard(Card.CardFromString("KH"));
    [ObservableProperty] public Stack _deck = MixedStack.ParseMixed("TC | AS 3C 4H TS");
}