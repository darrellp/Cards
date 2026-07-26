using CommunityToolkit.Mvvm.Input;

namespace SolitaireUI.ViewModels;

public interface IStatusBarViewModel
{
    IRelayCommand BackToGameSelectCommand { get; }
    IRelayCommand NewGameCommand { get; }
}
