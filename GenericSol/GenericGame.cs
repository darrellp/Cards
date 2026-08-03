using Cards;

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
            _undoHandler.AddMove((GenericMove)move, mix.CardsUp, GameState.State);
        }
        else
        {
            _undoHandler.AddMove((GenericMove)move, -1, GameState.State);
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

    public abstract IList<IMove> GetMoves();

    public abstract bool IsMoveValid(Stack stkSrc, string srcName, Stack stkDst, int cardCount);

    public virtual void StackDrop(Stack stkSrc, string srcName, Stack stkDst, int cardCount, int dragSrcCardsUp)
    {
        if (IsMoveValid(stkSrc, srcName, stkDst, cardCount))
        {
            var move = new GenericMove(srcName, stkDst.Name, cardCount);
            // Games which need to turn multiple stock transfers into a single undoable move can override CreateUndo to handle that.
            _undoHandler.StartUndo();
            _undoHandler.AddMove(move, dragSrcCardsUp, GameState.State);
            ApplyMove(move, stkSrc);
        }
    }

    public abstract Stack StackFromName(string name);
}
