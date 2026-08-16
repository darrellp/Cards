using Cards;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenericSol;
using GenericSol.Games.Klondike;
using System;

namespace SolitaireUI.ViewModels;

public partial class KlondikeViewModel : GameViewModelBase, IStatusBarViewModel
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    private static KlondikeGame _klondikeGameModel = new();
    private static IGame _game = _klondikeGameModel;

    public override IGame Game => _game;

    [ObservableProperty] private Stack _stock = _klondikeGameModel.StackFromName("stock");
    [ObservableProperty] private Stack _waste = _klondikeGameModel.StackFromName("waste");
    [ObservableProperty] private Stack _fnd1 = _klondikeGameModel.StackFromName("fnd1");
    [ObservableProperty] private Stack _fnd2 = _klondikeGameModel.StackFromName("fnd2");
    [ObservableProperty] private Stack _fnd3 = _klondikeGameModel.StackFromName("fnd3");
    [ObservableProperty] private Stack _fnd4 = _klondikeGameModel.StackFromName("fnd4");
    [ObservableProperty] private Stack _tab1 = _klondikeGameModel.StackFromName("tab1");
    [ObservableProperty] private Stack _tab2 = _klondikeGameModel.StackFromName("tab2");
    [ObservableProperty] private Stack _tab3 = _klondikeGameModel.StackFromName("tab3");
    [ObservableProperty] private Stack _tab4 = _klondikeGameModel.StackFromName("tab4");
    [ObservableProperty] private Stack _tab5 = _klondikeGameModel.StackFromName("tab5");
    [ObservableProperty] private Stack _tab6 = _klondikeGameModel.StackFromName("tab6");
    [ObservableProperty] private Stack _tab7 = _klondikeGameModel.StackFromName("tab7");

    public KlondikeViewModel(MainWindowViewModel mainWindowViewModel)
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
        _game = _klondikeGameModel = new KlondikeGame();
        SubscribeToGameEvents(_game);

        Stock = _klondikeGameModel.StackFromName("stock");
        Waste = _klondikeGameModel.StackFromName("waste");
        Fnd1 = _klondikeGameModel.StackFromName("fnd1");
        Fnd2 = _klondikeGameModel.StackFromName("fnd2");
        Fnd3 = _klondikeGameModel.StackFromName("fnd3");
        Fnd4 = _klondikeGameModel.StackFromName("fnd4");
        Tab1 = _klondikeGameModel.StackFromName("tab1");
        Tab2 = _klondikeGameModel.StackFromName("tab2");
        Tab3 = _klondikeGameModel.StackFromName("tab3");
        Tab4 = _klondikeGameModel.StackFromName("tab4");
        Tab5 = _klondikeGameModel.StackFromName("tab5");
        Tab6 = _klondikeGameModel.StackFromName("tab6");
        Tab7 = _klondikeGameModel.StackFromName("tab7");
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
}
