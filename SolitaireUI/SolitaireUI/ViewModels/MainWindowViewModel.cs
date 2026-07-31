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

    static Bitmap? FaceDownOverlapBitmap;
    static double FaceDownOverlapBitmapHeight = -1;
    static double FaceDownOverlapBitmapWidth = -1;
    static double FaceDownOverlapBitmapScaling = -1;

    /// <summary>
    /// Returns a bitmap containing the top slice of the card back image, <paramref name="height"/>
    /// device-independent units high (as measured against a card drawn at <paramref name="cardWidth"/> x
    /// <paramref name="cardHeight"/>). Used to draw the overlapping portions of face-down cards in a
    /// <see cref="MixedStack"/> using the actual card back artwork instead of a placeholder color.
    /// </summary>
    /// <param name="renderScaling">
    /// The current visual's render scaling (DPI factor). The backing bitmap is rendered at this
    /// scale so it isn't upsampled/blurred when displayed on high-DPI screens.
    /// </param>
    static public Bitmap GetFaceDownOverlapBitmap(double height, double cardWidth, double cardHeight, double renderScaling)
    {
        EnsureImagesLoaded();

        if (FaceDownOverlapBitmap == null
            || FaceDownOverlapBitmapHeight != height
            || FaceDownOverlapBitmapWidth != cardWidth
            || FaceDownOverlapBitmapScaling != renderScaling)
        {
            var fullBack = CardBackImage!;
            var sourceHeight = fullBack.Size.Height * (height / cardHeight);
            var sourceRect = new Rect(0, 0, fullBack.Size.Width, sourceHeight);
            var destRect = new Rect(0, 0, cardWidth, height);

            // Render at the screen's actual scaling factor (not just 1 device pixel per DIP) so the
            // slice isn't a low-resolution bitmap that then gets blurrily upscaled by the compositor.
            var pixelSize = new PixelSize(
                Math.Max(1, (int)Math.Ceiling(cardWidth * renderScaling)),
                Math.Max(1, (int)Math.Ceiling(height * renderScaling)));
            var dpi = new Vector(96 * renderScaling, 96 * renderScaling);

            var renderTarget = new RenderTargetBitmap(pixelSize, dpi);
            using (var context = renderTarget.CreateDrawingContext())
            {
                context.DrawImage(fullBack, sourceRect, destRect);
            }

            FaceDownOverlapBitmap = renderTarget;
            FaceDownOverlapBitmapHeight = height;
            FaceDownOverlapBitmapWidth = cardWidth;
            FaceDownOverlapBitmapScaling = renderScaling;
        }

        return FaceDownOverlapBitmap;
    }

    static Bitmap? FaceDownBackingBitmap;
    static double FaceDownBackingBitmapHeight = -1;
    static double FaceDownBackingBitmapWidth = -1;
    static double FaceDownBackingBitmapScaling = -1;

    // Fraction of the card's height, measured from the top, that the rounded corner artwork can
    // occupy. Cropping from at/after this point guarantees fully opaque pixels, since it's well
    // past the top rounded corners.
    private const double CornerSafeFraction = 0.3;

    /// <summary>
    /// Returns a bitmap the same size as <see cref="GetFaceDownOverlapBitmap"/> but cropped from
    /// further down the card back image, past its rounded top corners, so it's guaranteed opaque.
    /// Drawing this behind a non-topmost peek slice means its transparent rounded corners reveal
    /// this same card-back artwork instead of the playing surface underneath. (Stretching the same
    /// top crop used by <see cref="GetFaceDownOverlapBitmap"/> doesn't work because that crop's
    /// corners remain transparent no matter how much it's stretched vertically.)
    /// </summary>
    static public Bitmap GetFaceDownBackingBitmap(double height, double cardWidth, double cardHeight, double renderScaling)
    {
        EnsureImagesLoaded();

        if (FaceDownBackingBitmap == null
            || FaceDownBackingBitmapHeight != height
            || FaceDownBackingBitmapWidth != cardWidth
            || FaceDownBackingBitmapScaling != renderScaling)
        {
            var fullBack = CardBackImage!;
            var sourceHeight = fullBack.Size.Height * (height / cardHeight);
            var sourceTop = fullBack.Size.Height * CornerSafeFraction;
            var sourceRect = new Rect(0, sourceTop, fullBack.Size.Width, sourceHeight);
            var destRect = new Rect(0, 0, cardWidth, height);

            var pixelSize = new PixelSize(
                Math.Max(1, (int)Math.Ceiling(cardWidth * renderScaling)),
                Math.Max(1, (int)Math.Ceiling(height * renderScaling)));
            var dpi = new Vector(96 * renderScaling, 96 * renderScaling);

            var renderTarget = new RenderTargetBitmap(pixelSize, dpi);
            using (var context = renderTarget.CreateDrawingContext())
            {
                context.DrawImage(fullBack, sourceRect, destRect);
            }

            FaceDownBackingBitmap = renderTarget;
            FaceDownBackingBitmapHeight = height;
            FaceDownBackingBitmapWidth = cardWidth;
            FaceDownBackingBitmapScaling = renderScaling;
        }

        return FaceDownBackingBitmap;
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
