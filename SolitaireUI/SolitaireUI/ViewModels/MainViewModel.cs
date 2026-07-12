using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Cards;

namespace SolitaireUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Welcome to Avalonia!";
    public Bitmap HeartCardImage { get; } = new Bitmap(Card.CardFromString("2H").ImageStream());
}