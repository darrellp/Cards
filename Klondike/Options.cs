namespace Klondike;

public class Options
{
    public bool Thoughtful { get; set; } = false;
    public int FeedTurnover { get; set; } = 3;
    public int Passes { get; set; } = int.MaxValue;
}