namespace GenericSol.Games.TestGame;

internal class TestAi : IAi
{
    public IGame Game { get; set; } = null!;

    public IMove GetNextMove()
    {
        return new GenericMove("From", "To");
    }
}
