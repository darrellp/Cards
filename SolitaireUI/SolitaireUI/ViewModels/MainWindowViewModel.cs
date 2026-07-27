using Avalonia.Media.Imaging;
using Cards;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;

// To create a new game a few things are needed:
//  1. Create a new ViewModel for the game in ViewModels/Games, inheriting from GameViewModelBase
//  2. Create a new View in Views/Games for the game, binding to the ViewModel
//  3. Add the game to the GameSelectViewModel's AvailableGames collection
//  4. Add navigation logic in MainWindowViewModel to switch to the new game's ViewModel
//  5. Add a command in the GameSelectViewModel to start the new game and navigate to its ViewModel
//  6. Add a new folder for it in GenericSol/Games and implement the game logic, inheriting from IGame

namespace SolitaireUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    static Bitmap[]? CardImages;
    static Bitmap? CardBackImage;

    static public Bitmap ImageFromCard(Card card)
    {
        EnsureImagesLoaded();
        return CardImages![card.Index];
    }

    static public Bitmap GetCardBackImage()
    {
        EnsureImagesLoaded();
        return CardBackImage!;
    }

    static private void EnsureImagesLoaded()
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
            CardBackImage = new Bitmap(Card.CardBackImage());
        }
    }

    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    private readonly GameSelectViewModel _gameSelectViewModel;
    private readonly KlondikeViewModel _klondikeViewModel;
    private readonly TestGameViewModel _testGameViewModel;

    public MainWindowViewModel()
    {
        _gameSelectViewModel = new GameSelectViewModel(this);
        _klondikeViewModel = new KlondikeViewModel(this);
        _testGameViewModel = new TestGameViewModel(this);

        // Start with game selection view
        _currentViewModel = _gameSelectViewModel;
    }

    public void NavigateToGameSelect()
    {
        CurrentViewModel = _gameSelectViewModel;
    }

    public void NavigateToKlondike()
    {
        CurrentViewModel = _klondikeViewModel;
    }

    public void NavigateToTestGame()
    {
        CurrentViewModel = _testGameViewModel;
    }
}
