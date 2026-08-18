using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Cards;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
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

    // Fraction of the card's height, measured from the top, that the rounded corner artwork can
    // occupy. Cropping from at/after this point guarantees fully opaque pixels, since it's well
    // past the top rounded corners.
    internal const double CornerSafeFraction = 0.3;

    /// <summary>
    /// Returns the source rect (in the native card-back image's own coordinate space) for the top
    /// slice of the card back image that is <paramref name="height"/> device-independent units high
    /// (as measured against a card drawn at <paramref name="cardWidth"/> x <paramref name="cardHeight"/>).
    /// Used to draw the overlapping portions of face-down cards in a <see cref="MixedStack"/>, drawn
    /// directly from the actual card back artwork instead of a placeholder color or an intermediate
    /// off-screen bitmap (which previously risked introducing its own scaling errors).
    /// </summary>
    static public Rect GetFaceDownOverlapSourceRect(double height, double cardWidth, double cardHeight)
    {
        EnsureImagesLoaded();
        var fullBack = CardBackImage!;
        var sourceHeight = fullBack.Size.Height * (height / cardHeight);
        return new Rect(0, 0, fullBack.Size.Width, sourceHeight);
    }

    /// <summary>
    /// Returns a source rect the same size as <see cref="GetFaceDownOverlapSourceRect"/> but cropped
    /// from further down the card back image, past its rounded top corners, so it's guaranteed
    /// opaque. Drawing this behind a non-topmost peek slice means its transparent rounded corners
    /// reveal this same card-back artwork instead of the playing surface underneath. (Stretching the
    /// same top crop used by <see cref="GetFaceDownOverlapSourceRect"/> doesn't work because that
    /// crop's corners remain transparent no matter how much it's stretched vertically.)
    /// </summary>
    static public Rect GetFaceDownBackingSourceRect(double height, double cardWidth, double cardHeight)
    {
        EnsureImagesLoaded();
        var fullBack = CardBackImage!;
        var sourceHeight = fullBack.Size.Height * (height / cardHeight);
        var sourceTop = fullBack.Size.Height * CornerSafeFraction;
        return new Rect(0, sourceTop, fullBack.Size.Width, sourceHeight);
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
    private readonly CardBackSelectViewModel _cardBackSelectViewModel;
    private readonly GameInfoViewModel _gameInfoViewModel;
    private ViewModelBase? _previousViewModel;

    public MainWindowViewModel()
    {
        _gameSelectViewModel = new GameSelectViewModel(this);
        _klondikeViewModel = new KlondikeViewModel(this);
        _testGameViewModel = new TestGameViewModel(this);
        _cardBackSelectViewModel = new CardBackSelectViewModel(this);
        _gameInfoViewModel = new GameInfoViewModel(this);

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

    public void NavigateToCardBackSelect()
    {
        _previousViewModel = _currentViewModel;
        CurrentViewModel = _cardBackSelectViewModel;
    }

    public void NavigateToGameInfo(GenericSol.IGame game)
    {
        _previousViewModel = _currentViewModel;
        _gameInfoViewModel.SetGame(game);
        CurrentViewModel = _gameInfoViewModel;
    }

    public void NavigateBack()
    {
        if (_previousViewModel != null)
        {
            CurrentViewModel = _previousViewModel;
            _previousViewModel = null;
        }
    }

    public void RefreshCardBackImage()
    {
        CardBackImage = new Bitmap(Card.CardBackImage());
    }
}
