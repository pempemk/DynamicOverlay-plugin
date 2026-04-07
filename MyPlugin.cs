using PluginContract;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MyPlugin;

/// <summary>
/// Example plugin — displays the current time.
/// Rename the class and namespace to match your plugin name,
/// then replace the implementation with your own logic.
/// </summary>
public class MyPlugin : IPlugin, IDisposable
{
    // Metadata shown in the plugin list and info dialog.
    public string  Name        => "My Plugin";
    public string  Description => "Short description of what this plugin does";
    public string  Version     => "1.0.0";
    public string  Author      => "Your Name";
    public string? Url         => null;

    // Declare which system resources this plugin needs.
    // The user will see these in the permission approval dialog.
    // Use PluginPermission.None if no special access is required.
    public PluginPermission RequiredPermissions => PluginPermission.None;

    private readonly DispatcherTimer _timer;
    private readonly List<TextBlock> _displays = new();

    public MyPlugin()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    // Icon shown in the plugin panel (recommended size: 48x48, CornerRadius: 8).
    public Border GetIcon() => new()
    {
        Width        = 48,
        Height       = 48,
        Background   = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
        CornerRadius = new CornerRadius(8),
        Child = new TextBlock
        {
            Text                = "P",
            FontSize            = 22,
            FontWeight          = FontWeights.Bold,
            Foreground          = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
        }
    };

    // The app calls GetUIElement(mode) to render plugin content in the overlay.
    // mode indicates how many plugins share the overlay at the same time:
    //   Full  = 1 plugin  (widest)
    //   Half  = 2 plugins
    //   Third = 3 plugins (narrowest)
    public UIElement GetUIElement()                => BuildUI(LayoutMode.Full);
    public UIElement GetUIElement(LayoutMode mode) => BuildUI(mode);

    private UIElement BuildUI(LayoutMode mode)
    {
        double fontSize = mode switch
        {
            LayoutMode.Third => 14,
            LayoutMode.Half  => 18,
            _                => 24,
        };

        var text = new TextBlock
        {
            Text                = GetTimeString(),
            FontSize            = fontSize,
            FontWeight          = FontWeights.Bold,
            Foreground          = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
        };

        _displays.Add(text);
        return text;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        // DispatcherTimer fires on the UI thread — keep this callback fast.
        // For heavy work (I/O, network calls) use Task.Run and only
        // dispatch back to the UI thread when updating controls.
        string time = GetTimeString();
        foreach (var tb in _displays)
            tb.Text = time;
    }

    private static string GetTimeString() => DateTime.Now.ToString("HH:mm:ss");

    // Stop the timer and release resources when the plugin is disabled or removed.
    public void Dispose()
    {
        _timer.Stop();
        _displays.Clear();
    }
}
