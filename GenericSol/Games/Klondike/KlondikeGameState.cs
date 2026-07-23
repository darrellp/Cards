namespace GenericSol.Games.Klondike;
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
public class KlondikeGameState : GenericGameState
{
    public override string NewGameState(string gameEvent)
    {
        if (State == "Won" || State == "Lost")
        {
            return State;
        }

        //if (State == "WillWin" && gameEvent != "Win")
        //{
        //    // We stay in WillWin until we go into Win state
        //    return State;
        //}

        switch (gameEvent)
        {
            //case "WillWin":
            //    return "WillWin";
            case "NoMoves":
                return "NoMoves";

            case "Lose":
                return "Lost";

            case "EndOfStock":
                return State switch
                {
                    "NoMoves" => "Lost",
                    "AvoidedMoves" => "PlayingAvoidedMoves",
                    "PlayingAvoidedMoves" or "Moved" => "NoMoves",
                    _ => "Lost",
                };

            case "MadeMove":
                // Should we continue playing avoided moves for one run through the stock regardless?
                // i.e., if we're in PlayingAvoidedMoves state should we remain there until the end
                // of the current stock run?
                return "Moved";

            case "DetectedAvoidedMoves":
                if (State == "NoMoves")
                {
                    return "AvoidedMoves";
                }
                break;

            default:
                break;
        }
        return State;
    }

    override public string ToString() => State;
}