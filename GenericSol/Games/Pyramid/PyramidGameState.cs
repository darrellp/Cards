namespace GenericSol.Games.Pyramid;
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
//      NoMoves/Moved           Pyramid move made
//      NoMoves/Lost            End of stock
//      Moved/NoMoves           End of stock
//      
public class PyramidGameState : GenericGameState
{
    public override string NewGameState(string gameEvent)
    {
        if (State == "Won" || State == "Lost")
        {
            return State;
        }

        switch (gameEvent)
        {
            case "PyramidMove":
                return "Moved";

            case "Lose":
                return "Lost";

            case "EndOfStock":
                return State switch
                {
                    "NoMoves" => "Lost",
                    "Moved" => "NoMoves",
                    _ => "Lost",
                };

            default:
                break;
        }
        return State;
    }

    override public string ToString() => State;
}