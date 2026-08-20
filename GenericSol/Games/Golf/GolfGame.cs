using Cards;
using GenericSol.Games.Golf;
using GenericSol.Games.Klondike;
using System.Diagnostics;
using System.Security.Principal;

namespace GenericSol.Games.Golf;
public class GolfGame : GenericGame
{
    #region Constants
    public const int TabCount = 7;
    public const int TabSize = 5;
    #endregion

    GolfAi _ai;

    #region Stacks
    // These are only internal for unit testing purposes
    // ReSharper disable InconsistentNaming
    internal MixedStack[] _tableau { get; } = new MixedStack[TabCount];
    internal MixedStack _foundation;
    internal Stack _stock;
    // ReSharper restore InconsistentNaming

    internal IEnumerable<MixedStack> Tableaus()
    {
        for (var iStack = 0; iStack < TabCount; iStack++)
        {
            yield return _tableau[iStack];
        }
    }
    #endregion

    public override IAi Ai => (IAi)_ai;

    public override IList<IMove> GetMoves()
    {
        return new List<IMove> { new GenericMove("From", "To") };
    }

    #region Stack name helpers
    internal static string TabNameFromIndex(int index)
    {
        if (index < 0 || index >= TabCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Tableau index must be between 0 and 6.");
        }
        return $"tab{index + 1}";
    }

    public override Stack StackFromName(string name)
    {
        if (name.StartsWith("tab"))
        {
            var index = int.Parse(name.Substring(3));
            if (index > TabCount || index < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(name), "Tableau index must be between 0 and 6.");
            }
            return _tableau[index - 1];
        }
        else if (name == "foundation")
        {
            return _foundation;
        }
        else if (name == "stock")
        {
            return _stock;
        }
        else
        {
            throw new ArgumentException($"Invalid stack name: {name}");
        }
    }
    #endregion

    public GolfGame(int seed = -1) : base(seed)
    {
        _ai = new GolfAi();
        _ai.Game = this;
        var deck = Stack.ShuffledDeck();

        for (int i = 0; i < TabCount; i++)
        {
            _tableau[i] = MixedStack.FromStack(deck.Split(TabSize), TabSize);
            _tableau[i].Name = $"tab{i + 1}";
        }
        _foundation = new MixedStack(new List<Card>(), 0);
        _foundation.Name = "foundation";

        _stock = deck;
        _stock.Name = "stock";
    }

    #region Mouse Interaction
    public override void OnLeftClick(Stack stack)
    {
        if (stack.Count == 0)
        {
            return;
        }
        GenericMove finalMove = GenericMove.NoMove;
        if (stack.Name == "stock")
        {
            finalMove = new GenericMove("stock", "foundation");
        }
        else if (stack.Name.StartsWith("tab"))
        {
            if (_foundation.Count == 0)
            {
                finalMove = new GenericMove(stack.Name, "foundation");
            }
            else
            {
                finalMove = CheckStack(stack.Name);
            }
        }

        if (finalMove != GenericMove.NoMove)
        {
            ApplyMove(finalMove);
            if (Tableaus().Select(s => s.Count).Sum() == 0)
            {
                GameState.EventOccurred("Won");
            }
            else if (CheckLoss())
            {
                GameState.EventOccurred("Lost");
            }
        }
    }

    bool CheckLoss()
    {
        return _stock.Count == 0 && Enumerable.Range(1, 7).Select(i => $"tab{i}").All(n => CheckStack(n) == GenericMove.NoMove);
    }

    private GenericMove CheckStack(string stackName)
    {
        var tabStack = StackFromName(stackName);
        if (tabStack.Count == 0)
        {
            return GenericMove.NoMove;
        }
        var ret = GenericMove.NoMove;
        var card = tabStack.TopCard;
        // Ranks normally range from 1 to 13.  We want a range of 0 to 12.
        var zbRankSrc = card.Rank - 1;
        var zbRankDest = _foundation.TopCard.Rank - 1;
        if ((zbRankSrc + 1) % 13 == zbRankDest || (zbRankSrc + 12) % 13 == zbRankDest)
        {
            ret = new GenericMove(stackName, "foundation");
        }

        return ret;
    }

    public override bool IsMoveValid(Stack stkSrc, string srcName, Stack stkDst, int cardCount)
    {
        // We never drag in Golf - just left click
        return false;
    }
    #endregion

}
