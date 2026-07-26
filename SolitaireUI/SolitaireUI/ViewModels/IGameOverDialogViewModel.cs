using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;

namespace SolitaireUI.ViewModels;

public interface IGameOverDialogViewModel
{
    bool IsGameOverDialogVisible { get; }
    string GameOverMessage { get; }
    IBrush GameOverBackground { get; }
    IRelayCommand ResetGameCommand { get; }
}
