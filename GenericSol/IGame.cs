using Cards;

namespace GenericSol;

public interface IGame
{
    Random Random { get; }
    IAi Ai { get; }
    int Seed { get; }
    IGameState GameState { get; }
    int MoveCount { get; set; }
    Stack StackFromName(string name);
    String State { get; }
    IList<Stack> Stacks { get; }
    IList<IMove> GetMoves();
    void ApplyMove(IMove move);
}
