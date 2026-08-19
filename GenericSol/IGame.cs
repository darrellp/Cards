using Avalonia.Controls;
using Cards;
using System.Runtime.Serialization;

namespace GenericSol;

public interface IGame
{
    Random Random { get; }
    IAi Ai { get; }
    int Seed { get; }
    IGameState GameState { get; }
    int MoveCount { get; set; }
    Stack StackFromName(string name);
    String State { get; }
    IList<Stack> Stacks { get; }
    IList<IMove> GetMoves();
    void ApplyMove(IMove move, Stack? DragCards = null);
    bool IsMoveValid(Stack stkSrc, string srcName, Stack stkDst, int cardCount);
    void Undo();

    /// <summary>
    /// Setup options and rules for this game
    /// </summary>
    /// <param name="options">A grid to put the UI for the options into</param>
    /// <param name="markdown">Rules of the game</param>
    void SetupInfo(Grid options, out string markdown);

    /// <summary>
    /// Setup options from the UI placed into the grid by SetupInfo.  This is called when the user clicks OK on the options dialog.
    /// </summary>
    /// <param name="options">A grid containing the values for the options</param>
    void SetOptionsFromUI(Grid options);

    /// <summary>
    /// Setup options from a serializable object.
    /// </summary>
    /// <param name="options">A serializable object containing the values for the options</param>
    void SetOptions(IJsonSerializable options);

    /// <summary>
    /// Return the options in a serializable form
    /// </summary>
    /// <returns>A serializable object containing the values for the options</returns>
    IJsonSerializable GetOptions();

    /// <summary>
    /// Deserialize a JSON string, previously produced by <see cref="GetOptions"/>'s
    /// <see cref="IJsonSerializable.ToJson"/>, into an options object suitable for
    /// <see cref="SetOptions"/>. Returns null if this game has no options to deserialize.
    /// </summary>
    /// <param name="json">A JSON string representing this game's options</param>
    /// <returns>A deserialized options object, or null if this game has no options</returns>
    IJsonSerializable? DeserializeOptions(string json);
}
