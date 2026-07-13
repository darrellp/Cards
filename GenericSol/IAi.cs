namespace GenericSol;
public interface IAi
{
    IGame Game { get; set; }
    IMove GetNextMove();
}
