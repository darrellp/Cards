using Cards;

namespace GenericSol.Games.TestGame;
internal class TestGame : GenericGame
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
        var deck = Stack.ShuffledDeck(new Random(seed));
        _from = deck.Split(3);
        _to = new Stack([]);
    }
}
