namespace GenericSol;
public interface IGameState
{
    String State { get; set; }
    event EventHandler? Won;
    event EventHandler? Lost;
    void EventOccurred(string gameEvent);
}
