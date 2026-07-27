using Cards;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenericSol;
using GenericSol.Games.TestGame;
using System;

namespace SolitaireUI.ViewModels;

public partial class TestGameViewModel : GameViewModelBase, IStatusBarViewModel
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    private static TestGame _testGame = new();
    private static IGame _game = _testGame;

    public override IGame Game => _game;

    [ObservableProperty] private Stack? _from;
    [ObservableProperty] private Stack? _to;

    public TestGameViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _from = _testGame.StackFromName("From");
        _to = _testGame.StackFromName("To");
        SubscribeToGameEvents(_game);
    }

    [RelayCommand]
    private void ApplyAiMove()
    {
        var nextMove = _game.Ai.GetNextMove();
        if (nextMove is not null)
        {
            _game.ApplyMove(nextMove);
        }
    }

    protected override void ResetGameCore()
    {
        UnsubscribeFromGameEvents(_game);
        _game = _testGame = new TestGame();
        SubscribeToGameEvents(_game);

        From = _testGame.StackFromName("From");
        To = _testGame.StackFromName("To");
    }

    [RelayCommand]
    private void BackToGameSelect()
    {
        _mainWindowViewModel.NavigateToGameSelect();
    }

}
