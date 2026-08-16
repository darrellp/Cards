using CommunityToolkit.Mvvm.Input;

namespace SolitaireUI.ViewModels;

public interface IStatusBarViewModel
{
    IRelayCommand BackToGameSelectCommand { get; }
    IRelayCommand NewGameCommand { get; }
    IRelayCommand UndoCommand { get; }
    IRelayCommand CardBackSelectCommand { get; }
    string HoverStatusText { get; }
    string HoverCardText { get; }
    string GameState { get; }
}
