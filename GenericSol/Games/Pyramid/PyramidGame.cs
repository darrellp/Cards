using Cards;

namespace GenericSol.Games.PyramidGame;
public class PyramidGame : GenericGame
{
    Stack _from;
    Stack _to;
    PyramidAi _ai;

    public override IAi Ai => (IAi)_ai;

    public override IList<IMove> GetMoves()
    {
        return new List<IMove> { new GenericMove("From", "To") };
    }

    public override Stack StackFromName(string name)
    {
        return name == "From" ? _from : _to;
    }

    public PyramidGame(int seed = -1) : base(seed)
    {
        _ai = new PyramidAi();
        _ai.Game = this;
        var deck = Stack.ShuffledDeck();
        var stack = deck.Split(3);
        // The king ends on top as it was in the sorted deck, but we want it to be on the bottom of the stack
        stack.Reverse();
        _from = MixedStack.FromStack(stack, 3);
        _from.Name = "From";
        _to = new MixedStack([], 0);
        _to.Name = "To";
    }

    public override bool IsMoveValid(Stack stkSrc, string srcName, Stack stkDst, int cardCount)
    {
        return srcName == "From" && stkDst.Name == "To" && stkSrc.Count == 1;
    }

    // ApplyAbstractSplit runs after every split off of a stack, whether the move came from the AI
    // (via ApplyMove) or from a manual drag-and-drop (via StackDrop), so checking for a win
    // here - rather than in an ApplyMove override - ensures dragging the last card also
    // triggers the win state.
    public override void ApplyAbstractSplit(IMove move, Stack src, Stack moved, Stack dst)
    {
        if (_from.Count == 0)
        {
            GameState.EventOccurred("Won");
        }
    }
}
