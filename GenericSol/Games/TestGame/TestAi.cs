namespace GenericSol.Games.TestGame;

internal class TestAi : IAi
{
    public IGame Game { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public IMove GetNextMove()
    {
        return new GenericMove("From", "To");
    }
}
