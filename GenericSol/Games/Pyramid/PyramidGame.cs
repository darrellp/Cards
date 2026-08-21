using Cards;
using System.Drawing;

namespace GenericSol.Games.PyramidGame;
public class PyramidGame : GenericGame
{
    #region Stacks
    internal MixedStack[] _pyramid { get; } = new MixedStack[28];
    internal Stack _stock;
    internal Stack _waste;
    internal Stack _discards;

    // Pyramid is a 7-row triangle of cards, with 1 card in the top row, 2 in the second row, and so on down to 7 in the bottom row.
    // Navigating in pyramid is done by row and column, with the top card being at (0, 0) and the bottom row having cards at (6, 0) through (6, 6).

    public int PyramidIndex(int row, int col)
    {
        if (row < 0 || row > 6 || col < 0 || col > row)
        {
            return -1;
        }
        return row * (row + 1) / 2 + col;
    }

    internal static string PyramidNameFromIndex(int index)
    {
        if (index < 0 || index >= 28)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Pyramid index must be between 0 and 27.");
        }
        return $"pyr{index}";
    }

    public MixedStack[] PyramidStacks()
    {
        return _pyramid;
    }
    #endregion

    PyramidAi _ai;

    public override IAi Ai => (IAi)_ai;

    public override IList<IMove> GetMoves()
    {
        return new List<IMove> { new GenericMove("From", "To") };
    }

    public override Stack StackFromName(string name)
    {
        if (name.StartsWith("pyr"))
        {
            var index = int.Parse(name.Substring(3));
            if (index < 0 || index >= 28)
            {
                throw new ArgumentOutOfRangeException(nameof(name), "Pyramid index must be between 0 and 27.");
            }
            return _pyramid[index];
        }
        return name switch
        {
            "stock" => _stock,
            "waste" => _waste,
            "discards" => _discards,
            _ => throw new ArgumentOutOfRangeException(nameof(name), $"Unknown stack name: {name}")
        };
    }

    public override bool IsMoveValid(Stack stkSrc, string srcName, Stack stkDst, int cardCount)
    {
        return false;
    }

    public PyramidGame(int seed = -1) : base(seed)
    {
        _ai = new PyramidAi();
        _ai.Game = this;
        var deck = Stack.ShuffledDeck();

        for (var row = 0; row < 7; row++)
        {
            for (var col = 0; col <= row; col++)
            {
                var index = PyramidIndex(row, col);
                _pyramid[index] = MixedStack.FromStack(deck.Split(1), 1);
                _pyramid[index].Name = PyramidNameFromIndex(index);
            }
        }

        _stock = deck;
        _stock.Name = "stock";
        _waste = new Stack() { Name="waste" };
        _discards = new Stack() { Name="discards" };
    }

    #region Mouse Handling
    public override void OnLeftClick(Stack stack)
    {
        if (stack.Name.StartsWith("pyr"))
        {
            stack.SetHighlight(true, Color.LightBlue, 0, 1);
        }
    }
    #endregion
}
