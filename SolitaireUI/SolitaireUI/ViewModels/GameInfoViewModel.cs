using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenericSol;

namespace SolitaireUI.ViewModels;

public partial class GameInfoViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private IGame? _game;

    [ObservableProperty] private Grid _optionsGrid = new();
    [ObservableProperty] private string _markdown = string.Empty;

    public GameInfoViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
    }

    public void SetGame(IGame game)
    {
        _game = game;

        OptionsGrid.Children.Clear();
        OptionsGrid.RowDefinitions.Clear();
        OptionsGrid.ColumnDefinitions.Clear();

        _game.SetupInfo(OptionsGrid, out var markdown);
        Markdown = markdown;
    }

    [RelayCommand]
    private void Okay()
    {
        _game?.SetOptionsFromUI(OptionsGrid);
        _mainWindowViewModel.NavigateBack();
    }

    [RelayCommand]
    private void Cancel()
    {
        _mainWindowViewModel.NavigateBack();
    }
}
