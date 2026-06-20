using System.Diagnostics;
using Cards;

namespace Klondike;

#region Stack IDs
public enum StackId
{
    Tab1,
    Tab2,
    Tab3,
    Tab4,
    Tab5,
    Tab6,
    Tab7,
    Fnd1,
    Fnd2,
    Fnd3,
    Fnd4,
    Waste,
    Stock,
    None
}
#endregion



public class Game
{
    #region Public Members
    public readonly Options? GameOptions;
    public int Seed { get; }
    public const int TabCount = 7;
    public const int FndCount = 4;
    public int Moves { get; private set; }
    public bool Won => WinCheck();
    public bool Lost => State.Lost;

    public GameState State { get; } = new GameState();

    public int LowFoundationRank()
    {
        return Foundations().Select(s => s.Count == 0 ? 0 : s.TopCard.Rank).Min();
    }


    bool WinCheck()
    {
        if (!State.Won && _foundations.Select(s => s.Count).All(c => c == 13))
        {
            State.EventOccurred(Event.Win);
        }

        return State.Won;
    }
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
    
    #region Constructors
    public Game(int seed, Options? options = null)
    {
        Seed = seed;
        var rnd = seed < 0 ? new Random() : new Random(seed);
        var deck = Stack.ShuffledDeck(rnd);
        GameOptions = options ?? new Options();
        DealDeck(deck);
    }
    
    public Game(Stack deck)
    {
        DealDeck(deck);
    }

    public Game(Options? options = null) : this(-1, options) {}
    
    private void DealDeck(Stack deck)
    {
        Moves = 1;
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
    #endregion

    #region Move interpretation
    // ReSharper disable once MemberCanBePrivate.Global
    internal Stack StackFromId(StackId id)
    {
        return id switch
        {
            StackId.Stock => _stock,
            StackId.Waste => _waste,
            >= StackId.Tab1 and <= StackId.Tab7 => _tableau[id - StackId.Tab1],
            >= StackId.Fnd1 and <= StackId.Fnd4 => _foundations[id - StackId.Fnd1],
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    /// <summary>
    /// Return the source stack for a move
    /// </summary>
    /// <param name="move">Move to be queried</param>
    /// <returns>Source stack for the move</returns>
    public Stack FromStack(Move move)
    {
        return StackFromId(move.IdSrc);
    }
    
    /// <summary>
    /// Return the destination stack for a move
    /// </summary>
    /// <param name="move">Move to be queried</param>
    /// <returns>Destination stack for the move</returns>
    public Stack ToStack(Move move)
    {
        return StackFromId(move.IdDst);
    }

    /// <summary>
    /// Checks to see if a move is valid
    /// </summary>
    ///
    /// <remarks>
    /// This is only a check - the move is left unmade
    /// </remarks>
    /// 
    /// <param name="move">The move to check</param>
    /// 
    /// <returns>true if this is a valid move, false otherwise</returns>
    internal bool CheckMove(Move move)
    {
        var stackSrc = StackFromId(move.IdSrc);
        var stackDst = StackFromId(move.IdDst);

        if (move.CardCount != 1)
        {
            // Indexed moves can only be made from a tableaux stack
            if (move.IdSrc is < StackId.Tab1 or > StackId.Tab7)
            {
                return false;
            }
        }

        // Determine the source card
        Card crdSrc;
        
        if (move.IdSrc is >= StackId.Tab1 and <= StackId.Tab7)
        {
            crdSrc = stackSrc.Count == 0
                ? Card.NullCard
                : stackSrc[^move.CardCount];
        }
        else
        {
            Debug.Assert(move.CardCount == 0);
            crdSrc = stackSrc.TopCard;
        }
        
        if (crdSrc == Card.NullCard)
        {
            return false;
        }

        var ret = move.IdDst switch
        {
            // Can't move a single card from anywhere to feed
            StackId.Stock => false,

            // We can always move a card from feed to discard assuming there is a card
            // (empty feed will result in a null source card and so return false above).
            StackId.Waste => move.IdSrc == StackId.Stock,

            >= StackId.Tab1 and <= StackId.Tab7 =>
                stackDst.Count == 0 && crdSrc.Rank == Card.KING || crdSrc.IsKBelow(stackDst.TopCard),

            >= StackId.Fnd1 and <= StackId.Fnd4 => CheckFoundationMove(crdSrc, move.IdDst),
            _ => throw new ArgumentOutOfRangeException()
        };

        return ret;
    }
    #endregion

    #region Foundation sorting
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

    /// <summary>
    /// Return a suitable foundation stack for a given suit
    /// </summary>
    /// 
    /// <param name="suit">Suit we're querying about</param>
    /// 
    /// <returns>A stack which will accept the suit</returns>
    internal Stack FoundationFromSuit(Suit suit)
    {
        return _foundations[FndIndexFromSuit(suit)];
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
#endregion

    #region Stack accessing
    internal IEnumerable<MixedStack> Tableaus()
    {
        for (var iStack = 0; iStack < TabCount; iStack++)
        {
            yield return _tableau[iStack];
        }
    }

    public Stack Foundation(int iFnd)
    {
        return _foundations[iFnd];
    }

    public MixedStack Tableau(int iTab)
    {
        return _tableau[iTab];
    }

    public Stack Feed()
    {
        return _stock;
    }

    public Stack Discards()
    {
        return _waste;
    }
    
    internal IEnumerable<Stack> Foundations()
    {
        for (var iStack = 0; iStack < FndCount; iStack++)
        {
            yield return _foundations[iStack];
        }
    }
    #endregion

    #region Move validity checking
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
    
    /// <summary>
    /// See if a card can be played on a foundation stack
    /// </summary>
    /// 
    /// <param name="card">Card to check</param>
    /// <param name="idFnd">Proposed foundation stack ID</param>
    /// 
    /// <returns>true if card can be played, false otherwise</returns>
    private bool CheckFoundationMove(Card card, StackId idFnd)
    {
        Debug.Assert(idFnd is >= StackId.Fnd1 and <= StackId.Fnd4);
        var index = idFnd - StackId.Fnd1;
        var newFoundation =                                // To start new foundation:
            card.Rank == Card.ACE &&                            // Card played must be ace
            _fndSuits.All(s => s != card.Suit) &&           // Can't be another foundation with this suit
            _fndSuits[index] == Suit.None;                      // This foundation must be empty
        
        var buildFoundation =                              // To build a previous foundation
            _fndSuits[index] == card.Suit &&                    // Our suit must match the foundation's
            card.Rank == _foundations[index].TopCard.Rank + 1;  // Our rank must be one past the current top card's
        
        // Valid move if we're starting a new foundation or building on a previous one
        return newFoundation || buildFoundation;
    }
    #endregion
    
    #region Finding moves
    /// <summary>
    /// Find all possible moves
    /// </summary>
    ///
    /// <remarks>
    /// Moving cards from feed to discard or vice versa are always available so not listed here
    /// </remarks>
    /// 
    /// <returns>The list of moves</returns>
    internal List<Move> FindAllPossibleMoves()
    {
        var moves = new List<Move>();

        // Check moves from the discard pile
        if (_waste.Count != 0)
        {
            var discard = _waste.TopCard;
            
            // Check for discard to foundation moves
            var (fndIndex, fCanPlay) = CanPlayToFoundations(discard);
            if (fCanPlay)
            {
                // new foundation stack
                moves.Add(new Move(StackId.Waste, StackId.Fnd1 + fndIndex));
            }

            // Discard to tableau moves
            for (var iTab = 0; iTab < TabCount; iTab++)
            {
                // Check for moves from the discard pile to a tableau
                if (discard.IsKBelow(_tableau[iTab].TopCard))
                {
                    moves.Add(new Move(StackId.Waste, StackId.Tab1 + iTab));
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
                    moves.Add(new Move(StackId.Fnd1 + iFnd, StackId.Tab1 + iTabDst));
                }
            }
        }
        
        // Moves from Tableau
        for (var iTab = 0; iTab < TabCount; iTab++)
        {
            var tabCard = _tableau[iTab].TopCard;
            var tabId = StackId.Tab1 + iTab;
            
            // Tableau to foundation - here iTab tableau is source
            var (fndIndex, fCanPlay) = CanPlayToFoundations(tabCard);
            if (fCanPlay)
            {
                moves.Add(new Move(StackId.Tab1 + iTab, StackId.Fnd1 + fndIndex));
            }
            
            // tableau to tableau
            // Note: in this context iTab is the destination and we check over all other
            // possible source tableaus
            for (var iTabSrc = 0; iTabSrc < TabCount; iTabSrc++)
            {
                var (cCards, canPlay) = CanPlayTabToTab(iTabSrc, iTab);
                if (canPlay)
                {
                    var tabIdSrc = StackId.Tab1 + iTabSrc;
                    moves.Add(new Move(tabIdSrc, tabId, cCards));
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
        return moves;
    }
    #endregion
    
    #region Making moves
    /// <summary>
    /// Make a move
    /// </summary>
    ///
    /// <remarks>
    /// This call assumes that CheckMove(move) has already been called and returned true
    /// </remarks>
    /// 
    /// <param name="move">Move to be made</param>
    public void MakeMove(Move move)
    {
        if (Lost || Won)
        {
            return;     // No plays on won or lost games
        }
        var src = StackFromId(move.IdSrc);
        var dst = StackFromId(move.IdDst);
        
        if (move.ToFoundation)
        {
            // Mark this foundation stack as building in the source card's suit
            // (redundant after first ace but arguably faster to just do it than make a check)
            var index = move.IdDst - StackId.Fnd1;
            _fndSuits[index] = src.TopCard.Suit;
        }
        
        var movedCards = src.Split(move.CardCount);
        
        if (move.IdSrc == StackId.Stock || move.IdDst == StackId.Stock)
        {
            // If we've moving between the feed and discards or vice versa then the stack gets flipped
            movedCards.Reverse();
        }
        else
        {
            State.EventOccurred(Event.MadeMove);

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

        dst.Merge(movedCards);
        Moves++;
        
        if (move.IdDst == StackId.Stock)
        {
            State.EventOccurred(Event.EndOfStock);
        }
    }
    #endregion
    
    #region Invariants
    private int Invariant()
    {
        // I think maybe a suitable invariant is the
        //     number of faceup cards on the tableau +
        //     2 * number of foundation cards -
        //     2 times facedown cards in the tableaus +
        //     2 * the number of empty tableaus +
        //     2 * top level kings.
        var faceup = _tableau.Select(t => t.CardsUp).Sum();
        var foundations = _foundations.Select(f => f.Count).Sum();
        var facedown = _tableau.Select(t => t.Count).Sum() - faceup;
        var emptyTableaus = _tableau.Select(t => t.Count).Count(c => c == 0);
        var topLevelKings = _tableau.Count(t => t.Count != 0 && t.CardsUp == t.Count && t[0].Rank == Card.KING);
        return faceup + 2 * foundations - 2 * facedown + 2 * emptyTableaus + 2 * topLevelKings;
    }
    #endregion
    
    #region Playing a game
    public static bool PlayGame(int seed)
    {
        return new Game(seed).PlayGame();
    }

    public bool PlayGame()
    {
        return PlayGameTo(int.MaxValue);
    }

    internal bool PlayGameTo(int moveCount)
    {
        var ai = new AI(this);
        var invariantLast = Invariant();
        var iMove = 0;
        
        while (true)
        {
            if (++iMove >= moveCount)
            {
                Debugger.Break();
            }
            if (Won)
            {
                return true;
            }
            
            var move = ai.GetNextMove();
            if (Lost)
            {
                return false;
            }
            
            MakeMove(move);

            if (move.IdSrc != StackId.Stock && move.IdDst != StackId.Stock && move.comboMove == null)
            {
                var newInvariant = Invariant();
                Debug.Assert(newInvariant > invariantLast);
                invariantLast = newInvariant;
            }
        }
    }
    #endregion
}