# Dynamic Island Plugin Template

A starter template for building plugins for Dynamic Island WPF.

## Prerequisites

- .NET 9 SDK
- Visual Studio 2022 or Rider

## Setup

1. Clone this repository
2. Download `PluginContract.dll` from [Releases](https://github.com/pempemk/DynamicOverlay-plugin-template/releases) and place it in `lib/`
3. Rename the project to match your plugin name (both the `.csproj` filename and the namespace inside it)
4. Edit `MyPlugin.cs`
5. Build

## Installation for testing

```
Plugins/
  YourPluginName/
    YourPluginName.dll     <- must match the folder name
    SomeDependency.dll     <- additional DLLs (if any)
```

Copy the build output into `Plugins/<YourPluginName>/` and run the app.

## Further reading

See the full [Plugin Development Guide](https://github.com/pempemk/DynamicOverlay-plugin-template/blob/main/plugin-development.md) for details on layout modes, permissions, networking, and Steam Workshop publishing.
