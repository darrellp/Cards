using Cards;
using GenericSol.Games.Klondike;
using GenericSol.Games.Pyramid;
using System.Diagnostics;
using System.Drawing;

namespace GenericSol.Games.PyramidGame;
public class PyramidGame : GenericGame
{
    #region Stacks
    internal MixedStack[] _pyramid { get; } = new MixedStack[28];
    internal Stack _stock;
    internal Stack _waste;
    internal Stack _discards;
    public override IGameState GameState { get; set; } = new PyramidGameState();


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

    internal static int IndexFromPyramidName(string name)
    {
        if (!name.StartsWith("pyr"))
        {
            throw new ArgumentException("Invalid pyramid stack name", nameof(name));
        }
        var index = int.Parse(name.Substring(3));
        if (index < 0 || index >= 28)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "Pyramid index must be between 0 and 27.");
        }
        return index;
    }

    internal (MixedStack? left, MixedStack? right) GetPyramidChildren(int index)
    {
        if (index < 0 || index >= 28)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Pyramid index must be between 0 and 27.");
        }
        var row = (int)((Math.Sqrt(8 * index + 1) - 1) / 2);
        var col = index - row * (row + 1) / 2;
        if (row == 6)
        {
            return (null, null);
        }
        var leftChildIndex = PyramidIndex(row + 1, col);
        var rightChildIndex = PyramidIndex(row + 1, col + 1);
        var leftChild = _pyramid[leftChildIndex].Count > 0 ? _pyramid[leftChildIndex] : null;
        var rightChild = _pyramid[rightChildIndex].Count > 0 ? _pyramid[rightChildIndex] : null;
        return (leftChild, rightChild);
    }

    public MixedStack[] PyramidStacks()
    {
        return _pyramid;
    }
    #endregion

    #region Game fields
    Stack? PreviousSelection = null;

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
        // No moves are valid for dragging, so always return false.
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

    public override void ApplyAbstractPostMove(IMove move)
    {
        if (_pyramid != null && _pyramid.All(s => s.Count == 0))
        {
            GameState.EventOccurred("Won");
        }
        else if (move.SrcStack.StartsWith("pyr") || move.DstStack.StartsWith("pyr"))
        {
            GameState.EventOccurred("PyramidMove");
        }
        else if (move.SrcStack == "waste" && move.DstStack == "stock")
        {
            GameState.EventOccurred("EndOfStock");
        }
    }

    #region Mouse Handling
    public override void OnLeftClick(Stack stack)
    {
        if (stack.Name.StartsWith("pyr"))
        {
            var pyramidStack = stack as MixedStack;
            Debug.Assert(pyramidStack != null);
            if (pyramidStack == PreviousSelection)
            {
                UnsetPreviousSelection();
                return;
            }
            var (left, right) = GetPyramidChildren(IndexFromPyramidName(pyramidStack.Name));
            if (left != null || right != null)
            {
                // The card is not free to be removed, so ignore the click.
                return;
            }

            var cardSrc = pyramidStack.TopCard;
            if (cardSrc.Rank == Card.KING)
            {
                var move = new GenericMove(pyramidStack.Name, "discards");
                ApplyMove(move);
                UnsetPreviousSelection();
                return;
            }
            if (PreviousSelection == null)
            {
                SetPreviousSelection(pyramidStack);
                return;
            }
            if (MakeMove(pyramidStack))
            {
                return;
            }
            SetPreviousSelection(pyramidStack);
            return;
        }
        else if (stack.Name == "stock" || stack.Name == "waste")
        {
            if (PreviousSelection != null)
            {
                if (PreviousSelection.Name == stack.Name)
                {
                    UnsetPreviousSelection();
                }
                else
                {
                    MakeMove(stack);
                }
                return;
            }
            else if (stack.TopCard.Rank == Card.KING)
            {
                var move = new GenericMove(stack.Name, "discards");
                ApplyMove(move);
                UnsetPreviousSelection();
            }
            else if (stack.Count > 0)
            {
                SetPreviousSelection(stack);
            }
        }
    }

    public override void OnRightClick(Stack stack)
    {
        if (stack.Name == "stock")
        {
            if (stack.Count > 0)
            {
                var move = new GenericMove("stock", "waste", 1);
                ApplyMove(move);
                UnsetPreviousSelection();
            }
            else
            {
                var move = new GenericMove("waste", "stock", _waste.Count);
                ApplyMove(move);
                UnsetPreviousSelection();
            }
        }
        if (stack.Name == "waste")
        {
            if (stack.Count > 0 && _stock.Count == 0)
            {
                stack.Reverse();
                var move = new GenericMove("waste", "stock", stack.Count);
                ApplyMove(move);
                UnsetPreviousSelection();
            }
        }
    }

    // Makes a move if the previous selection and the current stack can be combined to make 13. Returns true if a move was made, false otherwise.
    bool MakeMove(Stack stk)
    {
        if (PreviousSelection != null && PreviousSelection.TopCard.Rank + stk.TopCard.Rank == 13)
        {
            var move = new GenericMove(PreviousSelection.Name, "discards", 1);
            ApplyMove(move);
            _startNewUndo = false;
            move = new GenericMove(stk.Name, "discards", 1);
            ApplyMove(move);
            _startNewUndo = true;
            UnsetPreviousSelection();
            return true;
        }
        return false;
    }

    void UnsetPreviousSelection()
    {
        if (PreviousSelection != null)
        {
            PreviousSelection.SetHighlight(false, Color.LightBlue, 0, 1);
            PreviousSelection = null;
        }
    }

    void SetPreviousSelection(Stack stack)
    {
        UnsetPreviousSelection();
        PreviousSelection = stack;
        stack.SetHighlight(true, Color.LightBlue, 0, 1);
    }
    #endregion
}
