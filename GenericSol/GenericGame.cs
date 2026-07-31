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

    public virtual void ApplyMove(IMove move, Stack? DragCards = null)
    {
        if (State == "Lost" || State == "Won")
        {
            return;     // No plays on won or lost games
        }

        var srcStack = DragCards ?? StackFromName(move.SrcStack);
        var dstStack = StackFromName(move.DstStack);
        var cardCount = move.CardCount;

        if (DragCards == null)
        {
            // Undo's are handled by the drag operation, so we don't need to do anything here.
            _undoHandler.StartUndo();
            CreateUndo(move);
        }

        ApplyAbstractPreMove(move);
        var movedCards = DragCards ?? srcStack.Split(cardCount);
        // TODO: Get rid of OnStackSplit
        //OnStackSplit(srcStack);
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
    /// 
    /// This is mostly archaic - it's functionality is now handled by <see cref="ApplyAbstractSplit(IMove, Stack, Stack, Stack)"/> 
    /// and <see cref="UndoSplitMove(GenericUndo, Stack, Stack, Stack)"/>.
    /// </summary>
    public virtual void OnStackSplit(Stack src) { }

    public abstract IList<IMove> GetMoves();

    public abstract bool IsMoveValid(Stack stkSrc, string srcName, Stack stkDst, int cardCount);

    public virtual void StackDrop(Stack stkSrc, string srcName, Stack stkDst, int cardCount, int dragSrcCardsUp)
    {
        if (IsMoveValid(stkSrc, srcName, stkDst, cardCount))
        {
            var move = new GenericMove(srcName, stkDst.Name, cardCount);
            // Games which need to turn multiple stock transfers into a single undoable move can override CreateUndo to handle that.
            _undoHandler.StartUndo();
            _undoHandler.AddMove(move, dragSrcCardsUp);
            ApplyMove(move, stkSrc);
        }
    }

    public abstract Stack StackFromName(string name);
}
