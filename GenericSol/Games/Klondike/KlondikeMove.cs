namespace GenericSol.Games.Klondike;
internal record KlondikeMove(string Src, string Dst, int Count = 1) : GenericMove(Src, Dst, Count)
{
    public KlondikeMove? comboMove = null;
    public bool FromTableau => Src.StartsWith("tab");
    public bool ToTableau => Dst.StartsWith("tab");
    public bool TabToTab => FromTableau && ToTableau;
    public bool FromFoundation => Src.StartsWith("fnd");
    public bool ToFoundation => Dst.StartsWith("fnd");
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    public static KlondikeMove NoMove = new KlondikeMove("NoSrc", "NoDst", 0);
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
}
