namespace GenericSol;

public record GenericMove(string Src, string Dst, int Count = 1) : IMove
{
    public string SrcStack { get; } = Src;
    public string DstStack { get; } = Dst;
    public int CardCount { get; } = Count;
    public static GenericMove NoMove = new GenericMove("NoSrc", "NoDst", 0);
}
