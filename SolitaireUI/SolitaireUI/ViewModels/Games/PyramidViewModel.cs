using Cards;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenericSol;
using GenericSol.Games.PyramidGame;
using System;

namespace SolitaireUI.ViewModels;

public partial class PyramidViewModel : GameViewModelBase, IStatusBarViewModel
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    private static PyramidGame _pyramidGame = new();
    private static IGame _game = _pyramidGame;

    public override IGame Game => _game;

    [ObservableProperty] private Stack? _from;
    [ObservableProperty] private Stack? _to;

    public PyramidViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _from = _pyramidGame.StackFromName("From");
        _to = _pyramidGame.StackFromName("To");
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
        _game = _pyramidGame = new PyramidGame();
        _mainWindowViewModel.ApplyStoredGameOptions(_game);
        SubscribeToGameEvents(_game);

        From = _pyramidGame.StackFromName("From");
        To = _pyramidGame.StackFromName("To");
    }

    [RelayCommand]
    private void BackToGameSelect()
    {
        _mainWindowViewModel.NavigateToGameSelect();
    }

    [RelayCommand]
    private void CardBackSelect()
    {
        _mainWindowViewModel.NavigateToCardBackSelect();
    }

    [RelayCommand]
    private void Undo()
    {
        _game.Undo();
    }

    [RelayCommand]
    private void Info()
    {
        _mainWindowViewModel.NavigateToGameInfo(_game);
    }
}
