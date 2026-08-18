using Avalonia.Controls;
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
    void ApplyMove(IMove move, Stack? DragCards = null);
    bool IsMoveValid(Stack stkSrc, string srcName, Stack stkDst, int cardCount);
    void Undo();
    void SetupInfo(Grid options, out string markdown);
    void SetOptions(Grid options);
}
