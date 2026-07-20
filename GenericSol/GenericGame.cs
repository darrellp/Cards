using Cards;

namespace GenericSol;
public abstract class GenericGame : IGame
{
    int _seed = -1;
    protected Random _random;

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

        ApplyAbstractPreMove(move);
        var movedCards = srcStack.Split(cardCount);
        ApplyAbstractSplit(move, srcStack, movedCards, dstStack);
        dstStack.Merge(movedCards);
        ApplyAbstractPostMove(move);
        MoveCount++;

    }

    public virtual void ApplyAbstractPreMove(IMove move) { }
    public virtual void ApplyAbstractSplit(IMove move, Stack src, Stack moved, Stack dst) { }
    public virtual void ApplyAbstractPostMove(IMove move) { }

    public abstract IList<IMove> GetMoves();

    public abstract Stack StackFromName(string name);
}
