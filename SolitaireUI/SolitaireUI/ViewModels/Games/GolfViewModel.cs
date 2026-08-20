using Cards;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenericSol;
using GenericSol.Games.Golf;
using System;

namespace SolitaireUI.ViewModels;

public partial class GolfViewModel : GameViewModelBase, IStatusBarViewModel
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    private static GolfGame _golfGameModel = new();
    private static IGame _game = _golfGameModel;

    public override IGame Game => _game;

    [ObservableProperty] private Stack _stock = _golfGameModel.StackFromName("stock");
    [ObservableProperty] private Stack _fnd = _golfGameModel.StackFromName("foundation");
    [ObservableProperty] private Stack _tab1 = _golfGameModel.StackFromName("tab1");
    [ObservableProperty] private Stack _tab2 = _golfGameModel.StackFromName("tab2");
    [ObservableProperty] private Stack _tab3 = _golfGameModel.StackFromName("tab3");
    [ObservableProperty] private Stack _tab4 = _golfGameModel.StackFromName("tab4");
    [ObservableProperty] private Stack _tab5 = _golfGameModel.StackFromName("tab5");
    [ObservableProperty] private Stack _tab6 = _golfGameModel.StackFromName("tab6");
    [ObservableProperty] private Stack _tab7 = _golfGameModel.StackFromName("tab7");

    public GolfViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        SubscribeToGameEvents(_game);
    }

    [RelayCommand]
    private void MakeAiMove()
    {
        var nextMove = _game.Ai.GetNextMove();
        if (nextMove is not null && nextMove.SrcStack != "NoSrc")
        {
            _game.ApplyMove(nextMove);
        }
    }

    protected override void ResetGameCore()
    {
        UnsubscribeFromGameEvents(_game);
        _game = _golfGameModel = new GolfGame();
        _mainWindowViewModel.ApplyStoredGameOptions(_game);
        SubscribeToGameEvents(_game);

        Stock = _golfGameModel.StackFromName("stock");
        Fnd = _golfGameModel.StackFromName("foundation");
        Tab1 = _golfGameModel.StackFromName("tab1");
        Tab2 = _golfGameModel.StackFromName("tab2");
        Tab3 = _golfGameModel.StackFromName("tab3");
        Tab4 = _golfGameModel.StackFromName("tab4");
        Tab5 = _golfGameModel.StackFromName("tab5");
        Tab6 = _golfGameModel.StackFromName("tab6");
        Tab7 = _golfGameModel.StackFromName("tab7");
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
