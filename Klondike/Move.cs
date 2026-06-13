using Cards;

namespace Klondike;

public record Move(StackId IdSrc, StackId IdDst, int CardCount = 1)
{
    public Move? comboMove = null;
    public bool GiveUpMove => CardCount < 0;
    public bool FromTableau => IdSrc is >= StackId.Tab1 and <= StackId.Tab7;
    public bool ToTableau => IdDst is >= StackId.Tab1 and <= StackId.Tab7;
    public bool TabToTab => FromTableau && ToTableau;
    public bool FromFoundation => IdSrc is >= StackId.Fnd1 and <= StackId.Fnd4;
    public bool ToFoundation => IdDst is >= StackId.Fnd1 and <= StackId.Fnd4;
    public static Move NoMove => new Move(StackId.None, 0);
}
