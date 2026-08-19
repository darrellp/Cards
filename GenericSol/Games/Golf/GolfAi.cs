namespace GenericSol.Games.Golf;

internal class GolfAi : IAi
{
    public IGame Game { get; set; } = null!;

    public IMove GetNextMove()
    {
        return new GenericMove("From", "To");
    }
}
