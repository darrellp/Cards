using Cards;
using System.Diagnostics;

namespace GenericSol.Games.Klondike;
internal class KlondikeAi : IAi
{
    #region Private fields
    // Place we can queue up moves so we can assure a series of
    // move are made in succession
    private readonly Queue<KlondikeMove> _nextMoves = new Queue<KlondikeMove>();
    #endregion

    public IGame Game { get; set; } = null!;
    private KlondikeGame KlondikeGame => (Game as KlondikeGame)!;

    public IMove GetNextMove()
    {
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

        var invariantMoves = Game.GetMoves().Where(m => CheckInvariant((KlondikeMove)m, KlondikeGame)).Cast<KlondikeMove>().ToList();
        var immediate = invariantMoves.FirstOrDefault(IsImmediateMove);
        List<KlondikeMove> moves;
        bool avoidMovesExist = false;

        if (immediate == null)
        {
            var (acceptableMoves, avoidMoves) = GetAvoids(invariantMoves);
            avoidMovesExist = avoidMoves.Count > 0;
            moves = acceptableMoves.Count == 0 && Game.State == "PlayingAvoidedMoves"
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
            if (Game.State != "Moved" && avoidMovesExist)
            {
                // We didn't have any usable moves in that stock run but detected some avoid moves
                Game.GameState.EventOccurred("DetectedAvoidedMoves");
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

    /// <summary>
    /// Pick the best move from a list of moves
    /// </summary>
    /// <param name="moves">The list of moves to select from</param>
    /// <returns>The best of all the moves - perhaps Move.NoMove if all moves in the list are avoided</returns>
    private KlondikeMove SelectBestMove(List<KlondikeMove> moves)
    {
        // Check for immediate moves
        var move = moves.FirstOrDefault(IsImmediateMove);
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

    private int SrcDepth(KlondikeMove? move)
    {
        while (move != null)
        {
            if (Game.StackFromName(move.Src) is MixedStack srcStack)
            {
                return srcStack.Count - srcStack.CardsUp;
            }
            move = move.comboMove;
        }

        return -1;
    }



    private bool IsRevealingMove(KlondikeMove? move)
    {
        while (move != null)
        {
            if (move.FromTableau)
            {
                var srcStack = KlondikeGame.StackFromName(move.Src) as MixedStack;
                Debug.Assert(srcStack != null, nameof(srcStack) + " != null");
                if (move.CardCount == srcStack.CardsUp && srcStack.Count > move.CardCount)
                {
                    return true;
                }
            }

            move = move.comboMove;
        }

        return false;
    }

    private void FlipFeed()
    {
        if (KlondikeGame._stock.Count == 0)
        {
            // Put waste back on stock - new stock run begins
            if (KlondikeGame._waste.Count == 0)
            {
                // If no waste cards to put on stock, we've lost
                KlondikeGame.GameState.EventOccurred("lose");
                _nextMoves.Enqueue(KlondikeMove.NoMove);
                return;
            }
            _nextMoves.Enqueue(new KlondikeMove("waste", "stock", KlondikeGame._waste.Count));
            //KlondikeGame.GameState.EventOccurred("EndOfStock");
            return;
        }

        var turnover = 3;
        var cardCount = Math.Min(turnover, KlondikeGame._stock.Count);
        _nextMoves.Enqueue(new KlondikeMove("stock", "waste", cardCount));
    }

    private (List<KlondikeMove> accept, List<KlondikeMove> avoid) GetAvoids(List<KlondikeMove> moves)
    {
        var accept = new List<KlondikeMove>();
        var avoid = new List<KlondikeMove>();

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

    // We classify moves into one of a few types:
    //   Immediate - return this as the move to make.  Nothing beats it.
    //   Rank - if no immediate moves then rank these moves and take the highest ranking
    //   Avoid - Do not make this move unless we've been through an entire run without making a move
    //   
    //   Immediate Moves - only one type - moving a card with rank <= lowest rank on foundations + 2
    //
    private bool IsImmediateMove(KlondikeMove move)
    {
        if (!move.ToFoundation)
        {
            return false;
        }
        var srcCard = KlondikeGame.StackFromName(move.Src).TopCard;
        var lowRank = KlondikeGame.LowFoundationRank(srcCard.Suit);
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
    private bool IsAvoidMove(KlondikeMove move)
    {
        // Don't open up a tableaux space unless there's a king to fill the new space or it's a king to
        // the foundation.  Don't want to label our final winning king moves as "avoid".
        if (move.FromTableau)
        {
            var srcStack = KlondikeGame.StackFromName(move.Src) as MixedStack;
            Debug.Assert(srcStack != null, nameof(srcStack) + " != null");
            if (srcStack.Count == srcStack.CardsUp && srcStack.Count == move.CardCount)
            {
                var kingsAvailable = false;

                // Is there a king on the waste pile?
                if (KlondikeGame._waste.TopCard.Rank == Card.KING)
                {
                    // Arrange for the king move to be next
                    move.comboMove = new KlondikeMove("waste", move.Src);
                    kingsAvailable = true;
                }

                // Is there a king on a tableau?
                for (var iTab = 0; iTab < KlondikeGame.TabCount; iTab++)
                {
                    var tab = KlondikeGame._tableau[iTab];
                    // We can only move a king from a tableau if it's got facedown cards below it
                    if (tab.CardsUp != tab.Count && tab.FirstFaceupCard.Rank == Card.KING)
                    {
                        var comboMoveCount = tab.CardsUp;
                        // If we are moving TO the king, we have to
                        // add our count to the cardcount to be moved since that will alter the King stack
                        if (move.Dst == KlondikeGame.TabNameFromIndex(iTab))
                        {
                            comboMoveCount += srcStack.CardsUp;
                        }

                        // Arrange for the king move to be next.
                        move.comboMove = new KlondikeMove(KlondikeGame.TabNameFromIndex(iTab), move.Src, comboMoveCount);
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

    /// <summary>
    /// Check a move to see if it breaks our invariant
    /// </summary>
    /// 
    /// <param name="move">the move to check</param>
    /// <param name="game">the game to check</param>
    /// <returns>True if it's fine, false if it breaks the invariant</returns>
    private static bool CheckInvariant(KlondikeMove move, KlondikeGame game)
    {
        if (move.TabToTab)
        {
            // Only move from a card in the middle of a tableau face up stack if the faceup card
            // revealed by the move will play to the foundations
            var src = game.StackFromName(move.Src) as MixedStack;
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
                move.comboMove = new KlondikeMove(move.Src, KlondikeGame.FndNameFromIndex(iFnd), 1);
            }
            return canPlay;
        }

        if (move.FromFoundation)
        {
            // Only move from a foundation if a following move will reveal a card or empty a tableau stack
            var dst = game.StackFromName(move.Dst) as MixedStack;
            var cardMove = game.StackFromName(move.Src).TopCard;
            Debug.Assert(dst != null, "Foundation destination isn't mixed stack (i.e., tableau)");

            for (var iTab = 0; iTab < KlondikeGame.TabCount; iTab++)
            {
                var tab = game._tableau[iTab];
                var moveCard = tab.FirstFaceupCard;
                if (moveCard.IsKBelow(cardMove))
                {
                    move.comboMove = new KlondikeMove(KlondikeGame.TabNameFromIndex(iTab), move.Dst, tab.CardsUp);

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

}
