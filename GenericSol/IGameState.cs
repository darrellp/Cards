namespace GenericSol;
public interface IGameState
{
    String State { get; }
    void EventOccurred(string gameEvent);
}
