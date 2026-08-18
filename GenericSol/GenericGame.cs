using Avalonia.Controls;
using Cards;
using GenericSol.Games.Klondike;

namespace GenericSol;
public abstract class GenericGame : IGame
{
    int _seed = -1;
    protected Random _random;
    protected GenericUndoHandler _undoHandler;
    string _turnState = "NoMoves";

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

    /// <summary>
    /// Applies a move to the game.
    /// </summary>
    /// 
    /// <remarks>
    /// We allow for a temporary stack to be the source.  This is primarily to accommodate drag and drop
    /// which may use a temporary stack to hold the cards being dragged.  The original source stack is used for undo purposes.
    /// This mechanism for dragging is a bit messy but it allows us to keep the move logic in one place and not have to 
    /// duplicate it for drag and drop.
    /// </remarks>
    /// 
    /// <param name="move">The move to apply</param>
    /// <param name="DragCards">The temporary stack containing the cards being dragged, if applicable</param>
    public virtual void ApplyMove(IMove move, Stack? DragCards = null)
    {
        if (State == "Lost" || State == "Won")
        {
            return;     // No plays on won or lost games
        }

        // The originating stack is the one originally moved or dragged from
        var origSrc = StackFromName(move.SrcStack);
        // For drags the srcStack will be the temporary drag stack distinct from the original source stack.
        var srcStack = DragCards ?? origSrc;
        var dstStack = StackFromName(move.DstStack);
        var cardCount = move.CardCount;

        _undoHandler.StartUndo();
        // If we're dragging then the move uses the temporary DragCards stack as a source
        // but for undo purposes we need to indicate the proper source
        var undoMove = new GenericMove(move.SrcStack, move.DstStack, move.CardCount);
        var cardsUp = -1;
        if (DragCards is not null && origSrc is MixedStack mix)
        {
            cardsUp = DragCards.Count + mix.CardsUp;
        }
        CreateUndo(undoMove, cardsUp);

        ApplyAbstractPreMove(move);
        var movedCards = DragCards ?? srcStack.Split(cardCount);
        ApplyAbstractSplit(move, srcStack, movedCards, dstStack);
        dstStack.Merge(movedCards);
        ApplyAbstractPostMove(move);
        MoveCount++;
        _turnState = State;
    }

    public virtual void CreateUndo(IMove move, int cardsUp = -1)
    {
        var srcStack = StackFromName(move.SrcStack);
        if (srcStack is MixedStack mix)
        {
            _undoHandler.AddMove((GenericMove)move, cardsUp >= 0 ? cardsUp : mix.CardsUp, _turnState);
        }
        else
        {
            _undoHandler.AddMove((GenericMove)move, -1, _turnState);
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
            ApplyMove(move, stkSrc);
        }
    }

    public abstract Stack StackFromName(string name);

    public virtual void SetupInfo(Grid options, out string markdown)
    {
        markdown = """
            # H1
            ### H3
            Hi - the rules are...
            Well, frankly I don't seem to have any **rules**!
            """;
    }

    public virtual void SetOptions(Grid options) { }
}
