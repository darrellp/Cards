namespace GenericSol;
public class GenericGameState : IGameState
{
    public string State { get; set; } = "Normal";

    public event EventHandler? Won;
    public event EventHandler? Lost;

    public void EventOccurred(string gameEvent)
    {
        State = gameEvent switch
        {
            "Won" => "Won",
            "Lost" => "Lost",
            _ => NewGameState(gameEvent)
        };

        if (State == "Won")
        {
            Won?.Invoke(this, EventArgs.Empty);
        }
        else if (State == "Lost")
        {
            Lost?.Invoke(this, EventArgs.Empty);
        }
    }

    // Won/Lost are handled generically.  Any other state changes are handled
    // by overriding this method in a derived class.  The default is to return the current state.
    public virtual string NewGameState(string gameEvent)
    {
        return State;
    }
}
