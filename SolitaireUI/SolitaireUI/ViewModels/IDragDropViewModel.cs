using Cards;
using GenericSol;

namespace SolitaireUI.ViewModels;

/// <summary>
/// Implemented by every game view model that hosts draggable <see cref="Cards.Stack"/> piles via
/// <see cref="SolitaireUI.Controls.StackControl"/>. StackControl talks to whichever view model is
/// its DataContext purely through this interface so it works regardless of which specific game
/// (Klondike, TestGame, etc.) is currently active.
/// </summary>
public interface IDragDropViewModel
{
    IGame Game { get; }

    Stack? CurrentHoverStack { get; }

    bool IsDragging { get; }

    void HandleStackRightClick(Stack stack);

    bool StartDrag(Stack sourceStack, int cardCount, double topLevelX, double topLevelY, double clickOffsetX, double clickOffsetY);

    void UpdateDragHover(Stack? hoverStack, double topLevelX, double topLevelY);

    void CompleteDrag();

    void CancelDrag();
}
