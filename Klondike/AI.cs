using System.Diagnostics;
using Cards;

namespace Klondike;

/// <summary>
/// The algorithm for determining the next move in a game of Klondike.
/// </summary>
///
/// <remarks>
/// I would like to come up with an invariant and a moving strategy so that the invariant is
/// increased after a single sequence of moves.  The invariant should be based solely on the
/// cards turned up at the moment.  If this is all true we can't get ourselves in an infinite
/// loop.  Some single moves from one stack to another are reversible and so can't increase the
/// invariant because reversing the move would then make for a decrease.  Hence we sometimes have
/// to consider sequences of moves and ensure that the sequence raises the invariant.  For instance,
/// playing from a foundation to a tableau is reversible so can't necessarily raise the invariant
/// by itself but normally that will be done solely so we can play from another tableau stack onto
/// the former foundation card revealing a new card.  If this combination raises the invariant then
/// that's fine as long as we always follow up the first move by the second.  This is the purpose of
/// the _nextMoves queue.  We can put combinations of moves into this queue and ensure that the entire
/// sequence will be performed in order thereby always pushing the invariant higher.
/// I think maybe a suitable invariant is the
///     number of faceup cards on the tableau +
///     2 * number of foundation cards -
///     2 times facedown cards in the tableaus +
///     the number of empty tableaus +
///     2 * top level kings.
/// This means that moving from waste to either tableaus or foundations will increase the value by one
/// or two.  Moving from tableau to foundations increases it by one.  Moving from foundation to tableau will
/// decrease it by one but if followed by a move revealing a face down card that will add 1 for the faceup
/// tableau card and 2 for one less face down card for a total of 3 which, when combined with the -1 for the
/// foundation card being played to the tableau leaves +2.
/// The other reversible and therefore tricky move is moving from the middle of a pile of faceup tableau
/// to another tableau stack.  This doesn't change the invariant at all but the only real reason to do it
/// is to place the newly available faceup card in the source pile on the foundations giveing a net gain of +1.
///
/// So the rules for loop free playing are that:
/// 
/// 1. every card moving from a foundation to a tableau pile must
/// be followed by a tableau move onto that card. This excludes the improbable strategy of moving a foundation
/// card to a tableau followed by a stock move to this card which finally allows another tableau card to play onto
/// it revealing a new tableau card.  Even this might be accommodated with enough analysis but I honestly don't
/// think it would make any significant difference in winning percentages.
///
/// 2. Any move from tableau to tableau that doesn't reveal a card must be followed immediately by the card left on
/// the tableau being moved to the foundations.
///
/// 3. Any other move is allowed unconditionally.
///
/// Flipping the Feed pile doesn't change the invariant and is always allowed.  One full run through the stock without
/// finding a viable move is considered a no-move situation - i.e., a lost game.
///
/// This invariant only ensures loop free playing - not GOOD playing and we still need to winnow out the best move
/// from the remaining list of available moves.
/// </remarks>

// ReSharper disable once InconsistentNaming
public class AI(Game game)
{
    #region Private fields
    // Place we can queue up moves so we can assure a series of
    // move are made in succession
    private readonly Queue<Move> _nextMoves = new Queue<Move>();
    private readonly List<Move> _avoidMoves = new List<Move>();
    #endregion

    #region Next move methods
    /// <summary>
    /// Get the best available move.  This is the only public method in AI.
    /// </summary>
    /// <returns>Best move to make</returns>
    public Move GetNextMove()
    {
        if (game.Won)
        {
           return Move.NoMove;
        }
        EnqueueNextMoves();
        return _nextMoves.Dequeue();
    }
    
    /// <summary>
    /// Selects from among several potential new moves and enques the best one
    /// </summary>
    ///
    /// <remarks>
    /// Sometimes more than one move may be queued up.  The series of moves is guaranteed to increase
    /// the invariant though it may go down in the middle of the series of moves.
    /// </remarks>
    private void EnqueueNextMoves()
    {
        if (_nextMoves.Count > 0)
        {
            // We have a move enforced in a series of moves
            return;
        }

        var invariantMoves = game.FindAllPossibleMoves().Where(m => CheckInvariant(m, game)).ToList();
        var immediate = invariantMoves.FirstOrDefault(IsImmediateMove);
        List<Move> moves;
        bool avoidMovesExist = false;

        if (immediate == null)
        {
            var (acceptableMoves, avoidMoves) = GetAvoids(invariantMoves);
            avoidMovesExist = avoidMoves.Count > 0;
            moves = acceptableMoves.Count == 0 && game.State.State == GS.PlayingAvoidedMoves
                ? avoidMoves
                : acceptableMoves;
        }
        else
        {
            moves = [immediate];
        }

        // If there are no moves try flipping another card from the feed
        if (moves.Count == 0)
        {
            if (game.State.State != GS.Moved && avoidMovesExist)
            {
                // We didn't have any usable moves in that stock run but detected some avoid moves
                game.State.EventOccurred(Event.DetectedAvoidedMoves);
            }
            FlipFeed();
            return;
        }
        
        var nextMove = SelectBestMove(moves);
        
        _nextMoves.Enqueue(nextMove);
        
        // If this is part of a combo then enqueue the ensuing combo moves
        while (nextMove.comboMove != null)
        {
            nextMove = nextMove.comboMove;
            _nextMoves.Enqueue(nextMove);
        }
    }
    #endregion
    
    #region Move Selection

    (List<Move> accept, List<Move> avoid) GetAvoids(List<Move> moves)
    {
        var accept = new List<Move>();
        var avoid = new List<Move>();
        
        foreach (var m in moves)
        {
            if (IsAvoidMove(m))
            {
                avoid.Add(m);
            }
            else
            {
                accept.Add(m);
            }
        }
        return (accept, avoid);
    }
    
    /// <summary>
    /// Pick the best move from a list of moves
    /// </summary>
    /// <param name="moves">The list of moves to select from</param>
    /// <returns>The best of all the moves - perhaps Move.NoMove if all moves in the list are avoided</returns>
    private Move SelectBestMove(List<Move> moves)
    {
        // Check for immediate moves
        var move = moves.FirstOrDefault(m => IsImmediateMove(m));
        if (move != null)
        {
            return move;
        }
        
        // At this point it becomes a bit more difficult to pick out a move.  We have a list of non-immediate moves which
        // all increase the invariant and are either not avoided or its time to play them.  Some considerations:
        // 1. Stuff that turns up new invariant cards is always good
        // 2. Turning up cards in tableaus from deeper stacks is better
        // 3. UNLESS we've got kings on Tableaus with cards above them which will fill empty stacks

        // Preferentially pick revealing moves with maximum depth
        var revealing = moves.Where(IsRevealingMove).MaxBy(SrcDepth);
        if (revealing != null)
        {
            return revealing;
        }
        
        return moves.First();
    }

    private bool IsRevealingMove(Move move)
    {
        while (move != null)
        {
            if (move.FromTableau)
            {
                var srcStack = game.FromStack(move) as MixedStack;
                if (move.CardCount == srcStack.CardsUp && srcStack.Count > move.CardCount)
                {
                    return true;
                }
            }

            move = move.comboMove;
        }

        return false;
    }

    private int SrcDepth(Move move)
    {
        var srcStack = game.FromStack(move) as MixedStack;
        Debug.Assert(srcStack != null);
        return srcStack.Count - srcStack.CardsUp;
    }
    
    // We classify moves into one of a few types:
    //   Immediate - return this as the move to make.  Nothing beats it.
    //   Rank - if no immediate moves then rank these moves and take the highest ranking
    //   Avoid - Do not make this move unless we've been through an entire run without making a move
    //   
    //   Immediate Moves - only one type - moving a card with rank <= lowest rank on foundations + 2
    //
    private bool IsImmediateMove(Move move)
    {
        if (!move.ToFoundation)
        {
            return false;
        }
        // It would be a teeny bit faster to determine this once in the caller and pass it into this
        // routine as a paramater rather than repeatedly calculating it here...
        var srcCard = game.StackFromId(move.IdSrc).TopCard;
        var lowRank = game.LowFoundationRank(srcCard.Suit);
        return srcCard.Rank <= lowRank + 2;
    }

    /// <summary>
    /// Checks to see if we wish to actively avoid making a move.
    /// </summary>
    ///
    /// <remarks>
    /// Note that this call may add a combo move to the incoming move if needed to make it acceptable
    /// </remarks>
    /// 
    /// <param name="move">The move to check</param>
    /// 
    /// <returns>True if it's to be avoided, false otherwise</returns>
    private bool IsAvoidMove(Move move)
    {
        // Don't open up a tableaux space unless there's a king to fill the new space or it's a king to
        // the foundation.  Don't want to label our final winning king moves as "avoid".
        if (move.FromTableau)
        {
            var srcStack = game.FromStack(move) as MixedStack;
            Debug.Assert(srcStack != null, nameof(srcStack) + " != null");
            if (srcStack.Count == srcStack.CardsUp && srcStack.Count == move.CardCount)
            {
                var kingsAvailable = false;
                
                // Is there a king on the waste pile?
                if (game._waste.TopCard.Rank == Card.KING)
                {
                    // Arrange for the king move to be next
                    move.comboMove = new Move(StackId.Waste, move.IdSrc, 1);
                    kingsAvailable = true;
                }

                // Is there a king on a tableau?
                for (var iTab = 0; iTab < Game.TabCount; iTab++)
                {
                    // TODO: see if there are multiple king moves and take from the one with the most cards down
                    var tab = game._tableau[iTab];
                    // We can only move a king from a tableau if it's got facedown cards below it
                    if (tab.CardsUp != tab.Count && tab.FirstFaceupCard.Rank == Card.KING)
                    {
                        var comboMoveCount = tab.CardsUp;
                        // If we are moving TO the king, we have to
                        // add our count to the cardcount to be moved since that will alter the King stack
                        if (move.IdDst == StackId.Tab1 + iTab)
                        {
                            comboMoveCount += srcStack.CardsUp;
                        }
                        
                        // Arrange for the king move to be next.
                        move.comboMove = new Move(StackId.Tab1 + iTab, move.IdSrc, comboMoveCount);
                        kingsAvailable = true;
                        break;
                    }
                }

                if (!kingsAvailable)
                {
                    // Clearing out a spot with no kings available so avoid this move
                    return true;
                }
            }
        }

        // I think this is a redundant call to IsImmediateMove since the only way we would be here
        // is if IsImmediateMove had failed earlier or we would have immediately made the move.  Still,
        // it's a short routine and this is probably the safest thing to do.
        if (move.ToFoundation && !IsImmediateMove(move))
        {
            // Foundation move above the foundation limits set up in IsImmediateMove
            return true;
        }
        
        return false;
    }
    #endregion

    #region Invariance methods
    /// <summary>
    /// Check a move to see if it breaks our invariant
    /// </summary>
    /// 
    /// <param name="move">the move to check</param>
    /// <param name="game">the game to check</param>
    /// <returns>True if it's fine, false if it breaks the invariant</returns>
    internal static bool CheckInvariant(Move move, Game game)
    {
        if (move.TabToTab)
        {
            // Only move from a card in the middle of a tableau face up stack if the faceup card
            // revealed by the move will play to the foundations
            var src = game.FromStack(move) as MixedStack;
            Debug.Assert(src != null, "Tab to Tab move invalid");
            if (move.CardCount == src.CardsUp)
            {
                // We're revealing a card in this stack (or emptying it) - allowed unless it is a first level King
                
                return src.FirstFaceupCard.Rank != Card.KING || src.Count > src.CardsUp;
            }
            Debug.Assert(move.CardCount < src.CardsUp, "Card count is incorrect");
            var cardAbove = src[^(move.CardCount + 1)];
            var (iFnd, canPlay) = game.CanPlayToFoundations(cardAbove);
            if (canPlay)
            {
                move.comboMove = new Move(move.IdSrc, StackId.Fnd1 + iFnd, 1);
            }
            return canPlay;
        }

        if (move.FromFoundation)
        {
            // Only move from a foundation if a following move will reveal a card or empty a tableau stack
            var dst = game.ToStack(move) as MixedStack;
            var cardMove = game.FromStack(move).TopCard;
            Debug.Assert(dst != null, "Foundation destination isn't mixed stack (i.e., tableau)");
            
            for (var iTab = 0; iTab < Game.TabCount; iTab++)
            {
                var tab = game._tableau[iTab];
                var moveCard = tab.FirstFaceupCard;
                if (moveCard.IsKBelow(cardMove))
                {
                    move.comboMove = new Move(StackId.Tab1 + iTab, move.IdDst, tab.CardsUp);

                    // TODO: There might be more multiple tabs that can be moved onto the fnd card but we're currently
                    // stopping at the first one we find.

                    // To fix this we need to make alterations to how we generate moves because one original move may
                    // branch to multiple combo moves.

                    return true;
                }
            }
            return false;
        }
        return true;
    }
    #endregion

    #region Stock and Waste interchanges
    private void FlipFeed()
    {
        if (game._stock.Count == 0)
        {
            // Put waste back on stock - new stock run begins
            if (game._waste.Count == 0)
            {
                // If no waste cards to put on stock, we've lost
                game.State.EventOccurred(Event.Lose);
                _nextMoves.Enqueue(Move.NoMove);
                return;
            }
            // TODO: Handle avoid moves
            _nextMoves.Enqueue(new Move(StackId.Waste, StackId.Stock, game._waste.Count));
            return;
        }

        var turnover = game.GameOptions?.FeedTurnover ?? 3;
        var cardCount = Math.Min(turnover, game._stock.Count);
        _nextMoves.Enqueue(new Move(StackId.Stock, StackId.Waste, cardCount));
    }
    #endregion
}