namespace GenericSol;
public interface IMove
{
    String SrcStack {  get; }
    String DstStack { get; }
    int CardCount { get; }
}
