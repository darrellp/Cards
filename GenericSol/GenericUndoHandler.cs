using Cards;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenericSol;

public class GenericUndoHandler
{
    internal Stack<List<GenericUndo>> UndoStack = new Stack<List<GenericUndo>>();

    public GenericGame Game { get; }

    internal void StartUndo()
    {
        UndoStack.Push(new List<GenericUndo>());
    }

    internal void AddMove(GenericMove move, int faceupPremove = -1, string state = "")
    {
        UndoStack.Peek().Add(new GenericUndo(move, faceupPremove, state));
    }

    internal void Undo()
    {
        if (UndoStack.Count == 0)
        {
            // Nothing to undo
            return;
        }
        var undoes = UndoStack.Pop();
        foreach (var undo in undoes)
        {
            Game.UndoPremove(undo);
            var srcStack = Game.StackFromName(undo.move.SrcStack);
            var dstStack = Game.StackFromName(undo.move.DstStack);
            var cardCount = undo.move.CardCount;
            var movedCards = dstStack.Split(cardCount);
            Game.UndoSplitMove(undo, srcStack, movedCards, dstStack);
            srcStack.Merge(movedCards);
            if (srcStack is MixedStack mix)
            {
                mix.CardsUp = undo.FaceupPremove;
            }
            Game.UndoPostMove(undo);
            // We set the gamestate raw here based on the assumption that any side
            // effects will be handled by the move undo machinery.
            Game.GameState.State = undo.State;
            Game.MoveCount--;
        }
    }

    public GenericUndoHandler(GenericGame Game)
    {
        this.Game = Game;
    }
}
