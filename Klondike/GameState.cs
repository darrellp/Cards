namespace Klondike;

#region Enums
// The game runs through a finite state machine which includes the following states:
//      NoMoves - No moves have been made in this run through the stock
//      Moved - a non-stock move has been made in this run through the stock
//      AvoidedMoves - avoided moves have been avoided and no regular moves have been made
//      PlayingAvoidedMoves - we are allowing avoided moves to be made
//      Won - the game is won
//      Lost - the game is lost
//
//      The game starts in NoMoves state and terminates in either Won or Lost states and the transitions are:
//      
//      From/To                 Transition
//      ----------------------------------
//      ANY/Won                 Game is won
//      NoMoves/Lost            End of stock
//      NoMoves/Moved           Any move is taken
//      NoMoves/AvoidedMoves    An avoided move is detected but not taken
//      AvoidedMoves/PlayingAvoidedMoves
//                              End of stock
//      PlayingAvoidedMoves/NoMoves
//                              End of stock
//      Moved/NoMoves           End of stock
//      
internal enum GS
{
    NoMoves,
    Moved,
    AvoidedMoves,
    PlayingAvoidedMoves,
    Won,
    Lost,
}

internal enum Event
{
    Win,
    Lose,
    MadeMove,
    DetectedAvoidedMoves,
    EndOfStock,
}
#endregion

internal class GameState
{
    internal GS State { get; set; } = GS.NoMoves;
    internal bool Lost => State == GS.Lost;
    internal bool Won => State == GS.Won;

    internal void EventOccurred(Event e)
    {
        if (State == GS.Won || State == GS.Lost)
        {
            // These are terminal states - no moving from them
            return;
        }
        
        switch (e)
        {
            case Event.Win:
                State = GS.Won;
                break;

            case Event.Lose:
                State = GS.Lost;
                break;

            case Event.EndOfStock:
                State = State switch
                {
                    GS.Moved => GS.NoMoves,
                    GS.NoMoves => GS.Lost,
                    GS.AvoidedMoves => GS.PlayingAvoidedMoves,
                    GS.PlayingAvoidedMoves or GS.Moved => GS.NoMoves,
                    _ => GS.Lost,
                };
                break;

            case Event.MadeMove:
                // Should we continue playing avoided moves for one run through the stock regardless?
                // i.e., if we're in PlayingAvoidedMoves state should we remain there until the end
                // of the current stock run?
                State = GS.Moved;
                break;

            case Event.DetectedAvoidedMoves:
                if (State == GS.NoMoves)
                {
                    State = GS.AvoidedMoves;
                }
                break;
        }
    }
}