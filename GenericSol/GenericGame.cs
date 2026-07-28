using Cards;
using System.ComponentModel.DataAnnotations;

namespace GenericSol;
public abstract class GenericGame : IGame
{
    int _seed = -1;
    protected Random _random;
    protected GenericUndoHandler _undoHandler;

    protected GenericGame(int seed = -1)
    {
        if (seed == -1)
        {
            _seed = new Random().Next();
        }
        else
        {
            _seed = seed;
        }
        _random = new Random(_seed);
        Initialize();
        _undoHandler = new GenericUndoHandler(this);
    }

    public virtual void Initialize() { }


    public Random Random => _random;
    public int Seed => _seed;
    public int MoveCount { get; set; }

    public string State => GameState.State;

    public IList<Stack> Stacks => throw new NotImplementedException();

    public virtual IAi Ai => throw new NotImplementedException();

    public virtual IGameState GameState { get; set; } = new GenericGameState();

    public virtual void ApplyMove(IMove move)
    {
        if (State == "Lost" || State == "Won")
        {
            return;     // No plays on won or lost games
        }

        var srcStack = StackFromName(move.SrcStack);
        var dstStack = StackFromName(move.DstStack);
        var cardCount = move.CardCount;

        _undoHandler.StartUndo();
        CreateUndo(move);
        ApplyAbstractPreMove(move);
        var movedCards = srcStack.Split(cardCount);
        OnStackSplit(srcStack);
        ApplyAbstractSplit(move, srcStack, movedCards, dstStack);
        dstStack.Merge(movedCards);
        ApplyAbstractPostMove(move);
        MoveCount++;
    }

    public virtual void CreateUndo(IMove move)
    {
        var srcStack = StackFromName(move.SrcStack);
        if (srcStack is MixedStack mix)
        {
            _undoHandler.AddMove((GenericMove)move, mix.CardsUp);
        }
        else
        {
            _undoHandler.AddMove((GenericMove)move);
        }
    }
    
    public virtual void Undo()
    {
        _undoHandler.Undo();
    }
    internal virtual void UndoPremove(GenericUndo undo) { }
    internal virtual void UndoSplitMove(GenericUndo undo, Stack src, Stack moved, Stack dst) { }
    internal virtual void UndoPostMove(GenericUndo undo) { }


    public virtual void ApplyAbstractPreMove(IMove move) { }
    public virtual void ApplyAbstractSplit(IMove move, Stack src, Stack moved, Stack dst) { }
    public virtual void ApplyAbstractPostMove(IMove move) { }
    public virtual void OnRightClick(Stack stack) { }
    public virtual void OnLeftClick(Stack stack) { }

    /// <summary>
    /// Called immediately after any cards are split off of a stack, regardless of whether the
    /// split originated from a normal move (<see cref="ApplyMove"/>) or from a UI drag operation.
    /// Games override this to handle bookkeeping such as flipping the next face-down card up
    /// once all face-up cards have been removed from a mixed stack.
    /// </summary>
    public virtual void OnStackSplit(Stack src) { }

    public abstract IList<IMove> GetMoves();

    public abstract bool IsMoveValid(Stack stkSrc, string srcName, Stack stkDst, int cardCount);

    public virtual void StackDrop(Stack stkSrc, string srcName, Stack stkDst, int cardCount, int dragSrcCardsUp)
    {
        if (IsMoveValid(stkSrc, srcName, stkDst, cardCount))
        {
            // TODO: Look into the following...
            // We'd like to go through ApplyMove here but that assumes that the source stack is intact but when
            // we're dragging the source stack has been split into the dragged and undragged portion.  ApplyMove
            // would do the undo machinery automagically but here we have to kind of do this by hand.  I'd like
            // to come up with a better solution since we may miss out on other ApplyMove side effects.  This may
            // come up to cause us problems in the future.

            var move = new GenericMove(srcName, stkDst.Name, cardCount);
            _undoHandler.StartUndo();
            _undoHandler.AddMove(move, dragSrcCardsUp);
            //ApplyMove(move);
            stkDst.Merge(stkSrc, cardCount);
            
            MoveCount++;
        }
    }

    public abstract Stack StackFromName(string name);
}
