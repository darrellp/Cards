namespace GenericSol.Games.PyramidGame;

internal class PyramidAi : IAi
{
    public IGame Game { get; set; } = null!;

    public IMove GetNextMove()
    {
        return new GenericMove("From", "To");
    }
}
