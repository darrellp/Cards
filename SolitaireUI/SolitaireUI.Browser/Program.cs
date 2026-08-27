using Avalonia;
using Avalonia.Browser;
using SolitaireUI;
using SolitaireUI.Browser;
using SolitaireUI.Services;
using System.Threading.Tasks;

internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        AppSettingsService.Store = new BrowserSettingsStore();

        return BuildAvaloniaApp()
            .WithInterFont()
#if DEBUG
            .WithDeveloperTools()
#endif
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}