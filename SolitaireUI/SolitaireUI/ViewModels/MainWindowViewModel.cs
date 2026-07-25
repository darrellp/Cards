using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SolitaireUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
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
