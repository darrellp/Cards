namespace GenericSol;
public interface IGameState
{
    String State { get; set; }
    event EventHandler? Won;
    event EventHandler? Lost;
    event EventHandler? StateChanged;
    void EventOccurred(string gameEvent);
}
