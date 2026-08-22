namespace GenericSol.Games.PyramidGame;

public class PyramidAi : IAi
{
    public IGame Game { get; set; } = null!;
    public PyramidGame PyramidGame => (PyramidGame)Game;

    public (IMove, IMove?) GetNextPair()
    {
        var moves = PyramidGame.FindAllMoves();
        if (moves.Count > 0)
        {
            return moves[0];
        }
        if (PyramidGame.Stock.Count > 0)
        {
            return (new GenericMove("stock", "waste"), null);
        }
        if (PyramidGame.Waste.Count > 0)
        {
            PyramidGame.Waste.Reverse();
            return (new GenericMove("waste", "stock", PyramidGame.Waste.Count), null);
        }
        return (GenericMove.NoMove, null);
    }

    IMove IAi.GetNextMove()
    {
        throw new NotImplementedException();
    }
}
