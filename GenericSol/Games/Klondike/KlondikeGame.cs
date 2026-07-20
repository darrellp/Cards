using Cards;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GenericSol.Games.Klondike;
public class KlondikeGame : GenericGame
{
    #region Constants
    public const int TabCount = 7;
    public const int FndCount = 4;
    #endregion

    #region Private members
    // Whether we have detected any moves yet in the current run through the feed
    private readonly Suit[] _fndSuits = [Suit.None, Suit.None, Suit.None, Suit.None];
    #endregion

    #region Stacks
    // These are only internal for unit testing purposes
    // ReSharper disable InconsistentNaming
    internal MixedStack[] _tableau { get; } = new MixedStack[TabCount];
    internal readonly Stack[] _foundations = new Stack[FndCount];
    internal Stack _waste { get; private set; } = new Stack();
    internal Stack _stock { get; private set; } = new Stack();
    // ReSharper restore InconsistentNaming
    #endregion

    internal IEnumerable<MixedStack> Tableaus()
    {
        for (var iStack = 0; iStack < TabCount; iStack++)
        {
            yield return _tableau[iStack];
        }
    }

    private void DealDeck(Stack deck)
    {
        Debug.Assert(deck.Count == 52);
        for (var iTab = 0; iTab < TabCount; iTab++)
        {
            _tableau[iTab] = MixedStack.FromStack(deck.Split(iTab + 1), 1);
        }

        for (var iFnd = 0; iFnd < FndCount; iFnd++)
        {
            _foundations[iFnd] = new Stack();
        }

        _waste = new Stack();
        _stock = deck;
    }

    override public void Initialize()
    {
        var deck = Stack.ShuffledDeck(_random);
        DealDeck(deck);
    }

    public int LowFoundationRank(Suit suit)
    {
        var top = int.MaxValue;
        var isBlack = Card.IsBlackSuit(suit);
        for (var iFnd = 0; iFnd < FndCount; iFnd++)
        {
            var fndSuit = _fndSuits[iFnd];
            if (fndSuit == Suit.None || Card.IsBlackSuit(fndSuit) != isBlack)
            {
                top = Math.Min(top, _foundations[iFnd].TopCard.Rank);
            }
        }

        return top;
    }

    public override IList<IMove> GetMoves()
    {
        var moves = new List<KlondikeMove>();

        // Check moves from the discard pile
        if (_waste.Count != 0)
        {
            var discard = _waste.TopCard;

            // Check for discard to foundation moves
            var (fndIndex, fCanPlay) = CanPlayToFoundations(discard);
            if (fCanPlay)
            {
                // new foundation stack
                moves.Add(new KlondikeMove("Waste", FndNameFromIndex(fndIndex)));
            }

            // Discard to tableau moves
            for (var iTab = 0; iTab < TabCount; iTab++)
            {
                // Check for moves from the discard pile to a tableau
                if (discard.IsKBelow(_tableau[iTab].TopCard))
                {
                    moves.Add(new KlondikeMove("Waste", TabNameFromIndex(iTab)));
                }
            }
        }

        // Moves from the foundations
        for (var iFnd = 0; iFnd < FndCount; iFnd++)
        {
            var fndCard = _foundations[iFnd].TopCard;
            for (var iTabDst = 0; iTabDst < TabCount; iTabDst++)
            {
                if (fndCard.IsKBelow(_tableau[iTabDst].TopCard))
                {
                    moves.Add(new KlondikeMove(TabNameFromIndex(iFnd), FndNameFromIndex(iTabDst)));
                }
            }
        }

        // Moves from Tableau
        for (var iTab = 0; iTab < TabCount; iTab++)
        {
            var tabCard = _tableau[iTab].TopCard;
            var tabId = TabNameFromIndex(iTab);

            // Tableau to foundation - here iTab tableau is source
            var (fndIndex, fCanPlay) = CanPlayToFoundations(tabCard);
            if (fCanPlay)
            {
                moves.Add(new KlondikeMove(TabNameFromIndex(iTab), FndNameFromIndex(fndIndex)));
            }

            // tableau to tableau
            // Note: in this context iTab is the destination and we check over all other
            // possible source tableaus
            for (var iTabSrc = 0; iTabSrc < TabCount; iTabSrc++)
            {
                var (cCards, canPlay) = CanPlayTabToTab(iTabSrc, iTab);
                if (canPlay)
                {
                    var tabIdSrc = TabNameFromIndex(iTabSrc);
                    moves.Add(new KlondikeMove(tabIdSrc, tabId, cCards));
                }
            }
        }

        // Right now I'm not considering Foundation to Tableau to avoid circular logic problems.  I probably should
        // include them and weed them out the same time I weed out circular dependencies in tableau to tableau moves.
        // Resolving these circularities I think depends on always turning one more card up.  We need sometimes to
        // think of one move as extending to two and the second always turns up a card or increases a foundation count.
        // So if we can play a 3S from under a 4H onto another tableau, the only time we'll do it is if the 4H can be
        // be played immediately thereafter onto the aces.  For the foundations to tableau we'll only do that if the
        // next move can be another tableau onto the former foundation card which results in turning up a card.
        // Really, this probably should be done a level above FindAllMoves which should echo it's name and find ALL
        // moves, leaving the weeding out to a higher level. 
        return (IList<IMove>)moves;
    }

    private (int cCards, bool canPLay) CanPlayTabToTab(int iSrc, int iDst)
    {
        var srcStack = _tableau[iSrc];
        var dstStack = _tableau[iDst];
        var noMove = (-1, false);

        // Can't play from tableau stack to the same tableau stack or move empty stack
        if (iSrc == iDst || srcStack.Count == 0)
        {
            return noMove;
        }

        // If the destination is empty we can only move a King
        if (dstStack.Count == 0)
        {
            return srcStack.FirstFaceupCard.Rank == Card.KING ? (srcStack.CardsUp, true) : noMove;
        }

        // BOTH STACKS ARE OCCUPIED

        var srcTop = srcStack.TopCard;
        var dstTop = dstStack.TopCard;

        // Can't place larger cards on smaller ones...
        if (srcTop.Rank >= dstTop.Rank)
        {
            return noMove;
        }

        var rankDifference = dstTop.Rank - srcTop.Rank;

        // We have to have enough cards in source pile to come up to proper rank for the dst pile
        if (srcStack.CardsUp < rankDifference)
        {
            return noMove;
        }

        // Finally, the colors have to be opposite
        if (srcStack[^rankDifference].IsSameColor(dstTop))
        {
            return noMove;
        }

        // If we've survived all the previous tests then it's a valid move
        return (rankDifference, true);
    }


    internal (int iFnd, bool canPlay) CanPlayToFoundations(Card card)
    {
        if (card == Card.NullCard)
        {
            return (-1, false);
        }

        var fndIndex = FndIndexFromSuit(card.Suit);
        var fndStack = _foundations[fndIndex];
        if (fndStack.Count == 0 && card.Rank == Card.ACE || fndStack.TopCard.Rank == card.Rank - 1)
        {
            return (fndIndex, true);
        }

        return (-1, false);
    }

    /// <summary>
    /// Find the index into _foundations of a suitable stack given a suit
    /// </summary>
    ///
    /// <remarks>
    /// If there already is a stack dedicated to this suit return it, otherwise return the first empty foundation
    /// </remarks>
    /// 
    /// <param name="suit">The suit we're searching for</param>
    /// 
    /// <returns>Index into _tableau which gives a suitable foundation for this suit</returns>
    private int FndIndexFromSuit(Suit suit)
    {
        var indexAssigned = -1;
        var indexUnassigned = -1;
        for (var i = 0; i < FndCount; i++)
        {
            if (_fndSuits[i] == suit)
            {
                indexAssigned = i;
                break;
            }
            if (_fndSuits[i] == Suit.None && indexUnassigned < 0)
            {
                indexUnassigned = i;
            }
        }

        Debug.Assert(indexAssigned >= 0 || indexUnassigned >= 0);
        return indexAssigned >= 0 ? indexAssigned : indexUnassigned;
    }

    internal static string FndNameFromIndex(int index)
    {
        if (index < 0 || index > FndCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Foundation index must be between 0 and 3.");
        }
        return $"fnd{index + 1}";
    }

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
        else if (name.StartsWith("fnd"))
        {
            var index = int.Parse(name.Substring(3));
            if (index > FndCount || index < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(name), "Foundation index must be between 0 and 3.");
            }
            return _foundations[index - 1];
        }
        else if (name == "stock")
        {
            return _stock;
        }
        else if (name == "waste")
        {
            return _waste;
        }
        else
        {
            throw new ArgumentException($"Invalid stack name: {name}");
        }
    }

    public override void ApplyAbstractPostMove(IMove move)
    {
        if (!WillWinCheck() && move.DstStack == "stock")
        {
            GameState.EventOccurred("EndOfStock");
        }

        WinCheck();
    }

    public override void ApplyAbstractPreMove(IMove move)
    {
        if (move.DstStack.StartsWith("fnd"))
        {
            // Mark this foundation stack as building in the source card's suit
            // (redundant after first ace but arguably faster to just do it than make a check)
            var index = int.Parse(move.DstStack.Substring(3)) - 1;
            var src = StackFromName(move.SrcStack);
            _fndSuits[index] = src.TopCard.Suit;
        }
    }

    public override void ApplyAbstractSplit(IMove move, Stack src, Stack moved, Stack dst)
    {
        if (move.SrcStack == "stock" || move.DstStack == "stock")
        {
            moved.Reverse();
        }
        else
        {
            GameState.EventOccurred("MadeMove");
        }

        if (src is MixedStack mixedStack)
        {
            // If we've cleared all the faceup cards and still have facedown cards then
            // turn one of them up
            if (mixedStack is { CardsUp: 0, Count: > 0 })
            {
                mixedStack.CardsUp = 1;
            }
        }
    }

    bool WinCheck()
    {
        if (State != "Won" && _foundations.Select(s => s.Count).All(c => c == 13))
        {
            GameState.EventOccurred("Won");
        }

        return GameState.State == "Won";
    }

    bool WillWinCheck()
    {
        if (_stock.Count == 0 && _waste.Count == 0 && Tableaus().All(s => s.Count == s.CardsUp))
        {
            GameState.EventOccurred("WillWin");
            return true;
        }

        return false;
    }


}
