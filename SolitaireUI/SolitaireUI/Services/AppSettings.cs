using System.Collections.Generic;
using System.Text.Json.Serialization;

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

/// <summary>
/// Source-generated JSON serialization context for <see cref="AppSettings"/>.
/// Reflection-based serialization is disabled in trimmed/AOT builds (e.g. the
/// Browser/WASM target), so a source-generated context is required.
/// </summary>
[JsonSerializable(typeof(AppSettings))]
public partial class AppSettingsJsonContext : JsonSerializerContext
{
}
