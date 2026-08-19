using System.Collections.Generic;

namespace SolitaireUI.Services;

/// <summary>
/// Persisted application settings: the selected card back and each game's serialized options,
/// keyed by the game model's type name (e.g. "KlondikeGame").
/// </summary>
public class AppSettings
{
    public int CardBackIndex { get; set; } = 1;

    public Dictionary<string, string> GameOptions { get; set; } = new();
}
