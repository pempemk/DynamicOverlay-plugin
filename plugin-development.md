# Plugin Development Guide

A guide for developers who want to build plugins for Dynamic Island WPF.

---

## Prerequisites

- .NET 9 SDK
- Visual Studio 2022, Rider, or VS Code
- `PluginContract.dll` — download from [Releases](https://github.com/pempemk/DynamicOverlay-plugin/releases)

---

## 1. Create a Project

Create a Class Library targeting `net9.0-windows` with WPF enabled:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows10.0.17763.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <AssemblyName>YourPluginName</AssemblyName>
    <RootNamespace>YourPluginName</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="PluginContract">
      <HintPath>lib\PluginContract.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

> `AssemblyName` must match the folder name under `Plugins/`.

---

## 2. Implement IPlugin

```csharp
public interface IPlugin
{
    string  Name        { get; }
    string  Description { get; }
    string  Version     { get; }
    string  Author      { get; }
    string? Url         { get; }

    PluginPermission RequiredPermissions => PluginPermission.None;

    UIElement GetUIElement();
    UIElement GetUIElement(LayoutMode mode) => GetUIElement();
    Border    GetIcon();
}
```

### Minimal example

```csharp
using PluginContract;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YourPluginName;

public class YourPluginName : IPlugin
{
    public string  Name        => "Plugin Name";
    public string  Description => "What this plugin does";
    public string  Version     => "1.0.0";
    public string  Author      => "Your Name";
    public string? Url         => null;

    public Border GetIcon() => new()
    {
        Width = 48, Height = 48,
        Background   = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
        CornerRadius = new CornerRadius(8),
        Child = new TextBlock
        {
            Text = "P", FontSize = 22, FontWeight = FontWeights.Bold,
            Foreground          = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
        }
    };

    public UIElement GetUIElement() => new TextBlock
    {
        Text       = "Hello World",
        Foreground = Brushes.White,
        FontSize   = 20,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment   = VerticalAlignment.Center,
    };
}
```

---

## 3. Layout Modes

The app calls `GetUIElement(mode)` where `mode` indicates how many plugins share the overlay:

| Mode    | Plugins in overlay | Approximate width |
|---------|--------------------|-------------------|
| `Full`  | 1                  | ~620px            |
| `Half`  | 2                  | ~310px            |
| `Third` | 3                  | ~200px            |

Design your UI to handle all three modes:

```csharp
public UIElement GetUIElement(LayoutMode mode)
{
    double fontSize = mode switch
    {
        LayoutMode.Third => 12,
        LayoutMode.Half  => 16,
        _                => 22,
    };
    // ...
}
```

---

## 4. Permissions

Declare any special resources your plugin needs:

```csharp
public PluginPermission RequiredPermissions =>
    PluginPermission.Network | PluginPermission.SystemInfo;
```

| Permission         | When to use                        |
|--------------------|------------------------------------|
| `None`             | No special access needed           |
| `Network`          | HTTP requests or WebSocket         |
| `FileSystem`       | Read or write files                |
| `SystemInfo`       | Read hardware or OS information    |
| `Clipboard`        | Read or write clipboard            |
| `ProcessExecution` | Launch external processes          |

The user will see a permission dialog before enabling the plugin.

---

## 5. Network — IPluginWithDomains

If your plugin makes HTTP requests, also implement `IPluginWithDomains`:

```csharp
public class YourPlugin : IPlugin, IPluginWithDomains
{
    public string[] AllowedDomains => new[]
    {
        "api.example.com",
        "cdn.example.com",
    };
}
```

Requests to domains not listed here will be blocked by the security layer.

---

## 6. Cleanup — IDisposable

If your plugin holds a timer or other resources, implement `IDisposable`:

```csharp
public class YourPlugin : IPlugin, IDisposable
{
    private readonly DispatcherTimer _timer;

    public YourPlugin()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}
```

---

## 7. Performance

`DispatcherTimer` fires on the UI thread. Do not perform slow operations inside its callback:

```csharp
// Incorrect — blocks the UI thread
private void OnTick(object? s, EventArgs e)
{
    var data = File.ReadAllText("large-file.txt");
    MyLabel.Text = data;
}

// Correct — heavy work runs on a background thread
private void OnTick(object? s, EventArgs e)
{
    Task.Run(async () =>
    {
        var data = await File.ReadAllTextAsync("large-file.txt");
        await Application.Current.Dispatcher.InvokeAsync(() =>
            MyLabel.Text = data);
    });
}
```

---

## 8. Packaging

The output folder must follow this structure:

```
YourPluginName/
  YourPluginName.dll      <- must match the folder name
  SomeDependency.dll      <- any additional DLLs go here
```

If your plugin has NuGet dependencies, add this to your `.csproj`:

```xml
<PropertyGroup>
  <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>
```

Then publish:

```
dotnet publish -c Release -o dist/YourPluginName
```

The contents of `dist/YourPluginName/` will include the main DLL and all dependencies.

---

## 9. Local Testing

Copy the output folder into the app's `Plugins/` directory:

```
<app directory>/
  Plugins/
    YourPluginName/
      YourPluginName.dll
```

Launch the app. The plugin will appear in the **Deactivated** panel automatically.

---

## 10. Publishing to Steam Workshop

1. Open the **Upload Plugin** dialog in the app (requires Steam login)
2. Select your plugin folder
3. Fill in title, description, and preview image
4. Click Upload

When a user subscribes, Steam downloads the plugin and the app copies it into `Plugins/` automatically.

---

## Template Project

Download the starter template at [DynamicOverlay-plugin-template](https://github.com/pempemk/DynamicOverlay-plugin)
