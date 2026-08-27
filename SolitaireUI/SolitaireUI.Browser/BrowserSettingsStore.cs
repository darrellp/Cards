using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using SolitaireUI.Services;

namespace SolitaireUI.Browser;

/// <summary>
/// Settings storage backend for the browser platform, using the browser's
/// localStorage since there is no real filesystem available in WASM.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed partial class BrowserSettingsStore : ISettingsStore
{
    private const string StorageKey = "SolitaireUI.settings";

    public string? Read() => GetItem(StorageKey);

    public void Write(string json) => SetItem(StorageKey, json);

    [JSImport("globalThis.localStorage.getItem")]
    private static partial string? GetItem(string key);

    [JSImport("globalThis.localStorage.setItem")]
    private static partial void SetItem(string key, string value);
}
