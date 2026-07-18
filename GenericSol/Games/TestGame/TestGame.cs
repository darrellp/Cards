using Cards;

namespace GenericSol.Games.TestGame;
public class TestGame : GenericGame
{
    Stack _from;
    Stack _to;
    TestAi _ai;

    public override IAi Ai => (IAi)_ai;

    public override IList<IMove> GetMoves()
    {
        return new List<IMove> { new GenericMove("From", "To") };
    }

    public override Stack StackFromName(string name)
    {
        return name == "From" ? _from : _to;
    }

    public TestGame(int seed = -1) : base(seed)
    {
        _ai = new TestAi();
        _ai.Game = this;
        var deck = Stack.ShuffledDeck();
        var stack = deck.Split(3);
        // The king ends on top as it was in the sorted deck, but we want it to be on the bottom of the stack
        stack.Reverse();
        _from = MixedStack.FromStack(stack, 3);
        _to = new MixedStack([], 0);
    }

    public override void ApplyMove(IMove move)
    {
        base.ApplyMove(move);
        if (_from.Count == 0)
        {
            GameState.EventOccurred("Won");
        }
    }
}
