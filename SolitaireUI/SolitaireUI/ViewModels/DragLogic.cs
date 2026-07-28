using Cards;
using GenericSol;
using System;
using System.Collections.Generic;
using System.Text;

namespace SolitaireUI.ViewModels;

public abstract partial class GameViewModelBase : ViewModelBase, IGameOverDialogViewModel, IDragDropViewModel
{
    int _dragSrcCardsUp;

    public bool StartDrag(Stack sourceStack, int cardCount, double topLevelX, double topLevelY, double clickOffsetX, double clickOffsetY)
    {
        if (sourceStack == null || cardCount <= 0 || cardCount > sourceStack.Count)
        {
            return false;
        }

        DragSourceStack = sourceStack;
        DragSourceName = sourceStack.Name;
        DragCardCount = cardCount;
        if (sourceStack is MixedStack mix)
        {
            _dragSrcCardsUp = mix.CardsUp;
        }
        var splitStack = sourceStack.Split(cardCount);
        // Render the drag ghost as a mixed stack with every card face up, regardless of how
        // the cards were represented in the source stack.
        TempDragStack = MixedStack.FromStack(splitStack, cardCount);

        // Note: unlike GenericGame.ApplyMove, we intentionally do NOT call game.OnStackSplit here.
        // When dragging with the mouse, the source stack should be left with 0 face-up cards
        // (rendered as a face-down peek) rather than auto-flipping the next card face up. The
        // flip only happens once the drag completes via StackDrop/CompleteDrag, or is reverted
        // on CancelDrag/invalid drop.
        CurrentHoverStack = null;
        DragOffsetX = clickOffsetX;
        DragOffsetY = clickOffsetY;
        DragX = topLevelX - clickOffsetX;
        DragY = topLevelY - clickOffsetY;
        return true;
    }

    public void UpdateDragHover(Stack? hoverStack, double topLevelX, double topLevelY)
    {
        DragX = topLevelX - DragOffsetX;
        DragY = topLevelY - DragOffsetY;

        if (TempDragStack == null || DragSourceName == null)
        {
            CurrentHoverStack = null;
            return;
        }

        // Don't hover over the source stack
        if (hoverStack != null && hoverStack == DragSourceStack)
        {
            CurrentHoverStack = null;
            return;
        }

        if (hoverStack != null && Game is GenericGame game)
        {
            if (game.IsMoveValid(TempDragStack, DragSourceName, hoverStack, DragCardCount))
            {
                CurrentHoverStack = hoverStack;
                return;
            }
        }

        CurrentHoverStack = null;
    }

    public void CompleteDrag()
    {
        if (TempDragStack == null || DragSourceStack == null || DragSourceName == null)
        {
            return;
        }

        if (CurrentHoverStack != null && Game is GenericGame game)
        {
            // Valid drop - commit the move and perform the same post-split bookkeeping
            // (e.g. flipping the next face-down card up) that an AI-driven move would.
            game.StackDrop(TempDragStack, DragSourceName, CurrentHoverStack, DragCardCount, _dragSrcCardsUp);
            game.OnStackSplit(DragSourceStack);
        }
        else
        {
            // Invalid drop - merge back to source; nothing was actually removed from the
            // source stack's perspective, so no flip bookkeeping is needed.
            DragSourceStack.Merge(TempDragStack);
        }

        // Clear drag state
        DragSourceStack = null;
        DragSourceName = null;
        TempDragStack = null;
        DragCardCount = 0;
        CurrentHoverStack = null;
    }

    public void CancelDrag()
    {
        if (TempDragStack != null && DragSourceStack != null)
        {
            DragSourceStack.Merge(TempDragStack);
        }

        DragSourceStack = null;
        DragSourceName = null;
        TempDragStack = null;
        DragCardCount = 0;
        CurrentHoverStack = null;
    }

    public bool IsDragging => TempDragStack != null;

}
