using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace SolitaireUI.ViewModels;

public partial class GameSelectViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    [ObservableProperty]
    private string? _selectedGame;

    public ObservableCollection<string> AvailableGames { get; }

    public GameSelectViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        AvailableGames = new ObservableCollection<string>
        {
            "Klondike",
            "Golf",
            "TestGame"
        };

        // Default selection
        _selectedGame = "Klondike";
    }

    [RelayCommand]
    private void StartGame()
    {
        if (SelectedGame == "Klondike")
        {
            _mainWindowViewModel.NavigateToKlondike();
        }
        else if (SelectedGame == "Golf")
        {
            _mainWindowViewModel.NavigateToGolf();
        }
        else if (SelectedGame == "TestGame")
        {
            _mainWindowViewModel.NavigateToTestGame();
        }
    }
}
