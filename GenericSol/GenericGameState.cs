namespace GenericSol;
internal class GenericGameState : IGameState
{
    public string State { get; set; } = "Normal";

    public void EventOccurred(string gameEvent)
    {
        State = gameEvent switch
        {
            "Won" => "Won",
            "Lost" => "Lost",
            _ => State
        };
}
}
