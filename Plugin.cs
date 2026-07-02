using DynamicOverlay.Contracts;

namespace MyPlugin;

/// <summary>
/// Your plugin's background logic. The UI lives in ui/index.html and talks to
/// this class through messages:
///
///   UI  → C#:  pluginApi.sendMessage("action", payload)  →  OnMessage(action, payload)
///   C#  → UI:  _ctx.SendToUI("action", payload)          →  window 'pluginMessage' event
///
/// This sample keeps a counter: the UI sends "increment", C# stores the new
/// value and pushes "countChanged" back so the UI re-renders.
/// </summary>
public sealed class Plugin : IPlugin
{
    /// <summary>Must match the "id" field in manifest.json exactly.</summary>
    public string Id => "com.yourname.myplugin";

    private IPluginContext? _ctx;
    private int _count;

    public void Initialize(IPluginContext context)
    {
        _ctx = context;

        // Runs once when the plugin starts (when the user places the tile on
        // the overlay bar). Good place to load saved state or start timers.
        //
        // If your manifest declares the "storage" permission you get a private
        // writable folder:
        //   string dir = context.GetStoragePath();
        //   File.WriteAllText(Path.Combine(dir, "state.json"), "...");
    }

    public void OnMessage(string action, string? payload)
    {
        if (_ctx is null) return;

        switch (action)
        {
            case "increment":
                // payload carries the user's "step" setting (see manifest "settings").
                _count += int.TryParse(payload, out var step) ? step : 1;
                _ctx.SendToUI("countChanged", _count.ToString());
                break;

            case "reset":
                _count = 0;
                _ctx.SendToUI("countChanged", "0");
                break;

            // Add your own actions here. payload is an arbitrary string —
            // use JSON for structured data.
        }
    }

    public void Dispose()
    {
        // Called when the plugin stops (tile removed from the bar, overlay
        // reloads, or the app exits). Stop timers and flush state here.
        _ctx = null;
    }
}
