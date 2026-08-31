# Dynamic Overlay Plugin Development Guide

A guide to building, testing, and publishing plugins for Dynamic Overlay.
This repository is a working template. Click **Use this template**, edit, build, done.

## 1. What a plugin is

```
your-plugin.dop (ZIP)
  manifest.json   id, name, version, entry point, permissions
  plugin.dll      your C# logic (IPlugin)
  ui/index.html   your tile UI (HTML/JS)
```

At runtime a plugin lives in two parts:

- **`ui/index.html`** renders as a tile on the overlay bar inside a WebView. It has no remote scripts, images, or direct network access, and talks to the outside world only through `window.pluginApi`.
- **`plugin.dll`** runs in a separate process. It has no UI of its own; it holds state, timers, and logic, and exchanges messages with your HTML.

The sample in this template is a **counter**: the UI sends `increment`, C# stores the value and pushes `countChanged` back to the UI.

## 2. Prerequisites

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Dynamic Overlay installed

The `DynamicOverlay.Sdk` NuGet package is already referenced in the project. `dotnet build` restores it for you, so there is nothing to download by hand.

## 3. Quick start

```powershell
# 1. Build. This also creates MyPlugin.dop in the output folder
dotnet build -c Release

# 2. Install for local testing (turn ON Developer Mode in Dynamic Overlay settings first)
copy bin\Release\net9.0-windows\MyPlugin.dop "$env:LocalAppData\DynamicOverlay\Plugins\"

# 3. Reload the overlay. Your plugin appears in the plugin list. Enable it.
```

Then make it yours:

1. **`manifest.json`**: change `id` (reverse-domain, globally unique) and `name`, and declare the `permissions` you need.
2. **`Plugin.cs`**: change the `Id` property to match the manifest, then replace the counter logic with your own.
3. **`ui/index.html`**: your tile's UI.
4. Rename `MyPlugin.csproj` if you like. The `.dop` is named after the project file.

## 4. What the SDK does for you

The single `<PackageReference Include="DynamicOverlay.Sdk" />` provides:

| What | Detail |
|---|---|
| The contract | `IPlugin` / `IPluginContext` to compile against. The host provides the same assembly at runtime, so never ship your own copy. |
| `AssemblyName` = `plugin` | Matches the manifest's `entryPoint: plugin.dll`. |
| Bundling | `manifest.json` and `ui/**` are copied into the build output. |
| `.dop` packing | After every build, packs `manifest.json`, the entry DLL, and `ui/` into `<ProjectName>.dop`. |

## 5. manifest.json reference

```json
{
  "id":          "com.yourname.myplugin",
  "name":        "My Plugin",
  "version":     "1.0.0",
  "entryPoint":  "plugin.dll",
  "ui":          "ui/index.html",
  "icon":        "ui/icon.png",
  "permissions": ["network"]
}
```

| Field | Required | Rules |
|---|---|---|
| `id` | yes | Reverse-domain style, globally unique. Starts with a letter or digit, then `A-Z a-z 0-9 . _ -`, max 128 chars. Used as your plugin's storage key, so keep it stable. |
| `name` | yes | Display name. Also the title of your `notify()` pills. |
| `version` | yes | SemVer string. |
| `entryPoint` | yes | A top-level `*.dll` filename (no subfolders). Keep it `plugin.dll` to match the SDK default. |
| `ui` | no | Path to your tile HTML. Default `ui/index.html`. |
| `icon` | no | Path to a PNG inside your package. Keep it: without an icon the plugin has no tile image in the plugin list, which makes local drag-to-test painful. |
| `permissions` | no | Names from the Permissions section. Case-insensitive. |
| `oauth` | no | Provider configs, only with the `oauth` permission. See the OAuth section. |
| `settings` | no | User-configurable options. See below. |

### 5a. User settings

Declare options and the host renders a form in your plugin's **Settings** dialog automatically. No UI work, no permission needed (only the `search` type needs `network`). The user's choices are delivered to your plugin via `pluginApi.getConfig().settings`.

```json
"settings": [
  { "key": "caption",     "label": "Caption",        "type": "text",   "default": "Count" },
  { "key": "step",        "label": "Increment step", "type": "number", "default": 1, "min": 1, "max": 100 },
  { "key": "theme",       "label": "Theme",          "type": "select", "default": "dark",
    "options": ["dark", "light", "neon"] },
  { "key": "showButtons", "label": "Show buttons",   "type": "toggle", "default": true }
]
```

#### Types

| `type` | Renders as | Value you read back | Extra fields |
|---|---|---|---|
| `toggle` | on/off switch | `true` / `false` | |
| `number` | numeric input | number | `min`, `max` |
| `text` | text box | string | |
| `select` | dropdown | the chosen string | `options` |
| `slider` | slider with a live value label | number | `min`, `max`, `step` (default 1) |
| `segmented` | pill buttons in a row | the chosen string | `options` (best for 2-4 short choices) |
| `color` | color swatch + hex box | hex string like `"#FF8800"` | |
| `multiselect` | checkbox list | array of the checked strings | `options` |
| `search` | search box with live suggestions | the picked item as a whole object | `sourceUrl`, `itemsPath`, `labelKey`, `subLabelKey` |
| `hotkey` | key-combination picker | `{ mods, vk, label }` | see below — the host binds it for you |

#### `hotkey`: a global shortcut, bound for you

Declare a setting of type `hotkey` and the host binds that combination system-wide. You do not register anything and you need no permission — when it fires, a `hotkey` event arrives naming the **setting key**, and you decide what it means:

```json
{ "key": "hkRefresh", "label": "Refresh now", "type": "hotkey",
  "default": { "mods": 0, "vk": 116, "label": "F5" } }
```

```js
window.addEventListener('hotkey', e => {
  if (e.detail.key === 'hkRefresh') refresh();
});
```

To let the user rebind it from inside your own UI, read the new combination with `captureHotkey()` and write it back with `setSetting()`. The binding follows the stored value, so your UI and the Settings dialog stay in sync:

```js
const hk = await pluginApi.captureHotkey();          // null on Escape/timeout
if (hk) await pluginApi.setSetting('hkRefresh', hk); // pass the whole object
```

Limit: **8 bound hotkeys per plugin**; anything past the eighth is ignored.

Two plugins may bind the same combination — both fire. The key also still reaches the focused app.

#### Common fields (any type)

| Field | Required | Notes |
|---|---|---|
| `key` | yes | Stable id you read back (e.g. `step`). |
| `type` | yes | One of the types above. Defaults to `toggle`. |
| `label` | no | Text shown in the form (defaults to `key`). |
| `default` | no | Value used until the user changes it. |
| `help` | no | Grey hint line shown under the control. |
| `group` | no | Section header text; consecutive settings with the same `group` appear together under it. |
| `dependsOn` | no | Key of another setting; this one only shows while that setting is on (truthy). |
| `dependsValue` | no | With `dependsOn`: show only while the other setting equals this exact value. |

#### `search`: pick from a live list

The host queries `sourceUrl` as the user types (`{query}` is replaced with the typed text), shows the results, and stores the item the user picks — the whole object, so your plugin gets every field of it. Requires the `network` permission.

```json
{ "key": "location", "label": "Location", "type": "search",
  "sourceUrl": "https://geocoding-api.open-meteo.com/v1/search?name={query}&count=5",
  "itemsPath": "results", "labelKey": "name", "subLabelKey": "country" }
```

- `itemsPath`: dot-path to the array of items inside the JSON response (e.g. `results`).
- `labelKey`: key on each item shown as the suggestion text; `subLabelKey`: optional dimmer sub-text.

```js
const s = (await pluginApi.getConfig()).settings;
// s.location is the picked object: { name, country, latitude, longitude, ... }
```

Read the values in your UI:

```js
const cfg = await pluginApi.getConfig();
const s = cfg.settings || {};
caption.textContent = s.caption;
if (s.showButtons === false) buttons.style.display = 'none';
// pass a value through to C# if you need it there:
pluginApi.sendMessage('increment', String(s.step));
```

Values apply when the plugin reloads. After the user hits **Save**, the overlay reloads so your plugin reads the new values.

## 6. The C# side (`IPlugin`)

```csharp
using DynamicOverlay.Contracts;

public sealed class Plugin : IPlugin
{
    public string Id => "com.yourname.myplugin";   // MUST match the manifest id

    private IPluginContext? _ctx;

    public void Initialize(IPluginContext context)
    {
        // Runs once when the user places your tile on the bar.
        // Load state and start timers here.
        _ctx = context;
    }

    public void OnMessage(string action, string? payload)
    {
        // Called when your UI does pluginApi.sendMessage(action, payload).
        // payload is an arbitrary string. Use JSON for structured data.
        if (action == "ping") _ctx?.SendToUI("pong", null);
    }

    public void Dispose()
    {
        // Tile removed, overlay reloading, or app closing.
        // Stop timers and flush state here.
    }
}
```

`IPluginContext` gives you:

| Member | Does |
|---|---|
| `SendToUI(action, payload)` | Push a message to your HTML. Arrives as a `pluginMessage` window event. |
| `Permissions` | The granted permission flags. |
| `GetStoragePath()` | A private writable folder for your plugin. Requires the `storage` permission. |

### UI to C# messaging

```
UI  to C# :  pluginApi.sendMessage("action", payload)   ->  Plugin.OnMessage(action, payload)
C#  to UI :  context.SendToUI("action", payload)        ->  window 'pluginMessage' event
```

```js
// receiving pushes in the UI
window.addEventListener('pluginMessage', e => {
  const { action, payload } = e.detail ?? {};
  if (action === 'pong') console.log('C# answered');
});
```

## 7. `window.pluginApi` reference (UI side)

All calls return Promises and reject with a readable error on failure or a missing permission. Default call timeout: 10 s.

`pluginApi.hasFeature(name)` reports whether this host knows an API. An older host ignores an option it does not understand and times out on a call it does not have, so guard anything newer than the app version you target:

```js
if (pluginApi.hasFeature('setOverlayStatus')) pluginApi.setOverlayStatus({ color: '#4a9eff', text: 'Syncing' });
```

### Core (no permission needed)

| Call | Does |
|---|---|
| `sendMessage(action, payload)` | Deliver to your C# `OnMessage`. |
| `notify(message, opts?)` | Overlay notification pill. `opts` takes `title` and `color`; the default title is your plugin's name. Rate-limited to 5 per 10 s. |
| `getData(key)` / `setData(key, value)` | Per-tile in-memory key-value store. Limits: 256 keys, 256 KB per value. Cleared when the tile is removed. |
| `getConfig()` | `{ name, version, settings }` from your manifest. `settings` holds the user's chosen values. |
| `setSetting(key, value)` | Write back one of your own declared settings. Same store the Settings dialog edits, so an in-plugin settings UI stays in sync with it. |
| `getLayout()` | `'full'` \| `'half'` \| `'third'` — the current slot. Pure JS, no round-trip, so it is safe to call often. |
| `setOverlayStatus({ color, text })` | Tint the **collapsed** overlay bar and put a short label on it, for reporting progress while your tile is not visible. Resolves `false` if another plugin already holds it. Pass `null` to release when you are done. |

### Files (no permission needed)

The user picks the location through a system dialog. Your plugin passes and receives file **contents only, never a path**, and there is no API to reopen a previously used file.

```js
const r = await pluginApi.saveFile(JSON.stringify(data), { name: 'export', ext: '.mydata' });
// { ok, name }   ok:false also covers the user cancelling

const text = await pluginApi.openFile({ ext: '.mydata' });              // contents as a string
const f    = await pluginApi.openFile({ ext: '.mydata', withName: true }); // { name, text }
```

`name` is the file name only — never a folder, never a full path.

### Persistent storage (needs `storage`)

| Call | Does |
|---|---|
| `getStore(key)` / `setStore(key, value)` / `removeStore(key)` | Disk-backed JSON that survives the tile being removed, the overlay reloading, and the app restarting. Use this instead of `setData` for anything that must outlive the session. |

### Network (needs `network`)

```js
const res = await pluginApi.request('https://api.example.com/data', {
  method: 'POST',          // default GET
  body: JSON.stringify(x), // sent as application/json (not for GET/HEAD)
  auth: 'spotify',         // optional: oauth provider name
  responseType: 'base64',  // optional: binary mode
});
// text mode:   { status, body }
// base64 mode: { status, body, contentType }   (max 8 MB)
```

- `http`/`https` only. Public hosts only, unless you hold `local-network`.
- Use `responseType: 'base64'` to fetch binary data and show it via a `data:` URI.

### Hardware / system (each gated by its permission)

| Call | Needs | Returns |
|---|---|---|
| `getSystemInfo()` | `system` | CPU / RAM usage snapshot |
| `getMedia()` | `media` | Now-playing: title, artist, playback state |
| `controlMedia(action)` | `media` | `action` is one of `play`, `pause`, `playpause`, `next`, `previous` |
| `getVolume()` | `audio` | `{ volume, mute }` |
| `setVolume({ volume?, mute? })` | `audio` | Set either or both |
| `getAudioSessions()` | `audio` | Per-app volume/mute, one entry per sounding app |
| `setAudioSession(pid, { volume?, mute? })` | `audio` | Set one app's volume/mute |
| `getMicState()` | `microphone` | `{ mute }` — current mute state |
| `setMicMute(mute)` | `microphone` | Mute/unmute the default capture device |
| `getBluetoothDevices()` | `bluetooth` | Paired devices (with battery where available) |
| `getHidDevices()` | `hid` | USB/HID peripherals |

### Input (each gated by its permission)

| Call | Needs | Returns |
|---|---|---|
| `readInput(cb, opts)` | `input-read` | `{ stop() }`. `cb` fires once per event. Stops collecting at 200,000 events without an error. |
| `sendInput(events, opts)` | `input-send` | `{ id, pause(), resume(), stop() }`. The event shape from `readInput` goes straight back in. |
| `captureHotkey(timeoutMs?)` | — | `{ mods, vk, label }`, or `null` on Escape or timeout. Default 10 s. |

`readInput` options: `keyboard`, `mouse` (both default `true`), `mouseMoveHz` (default `100`), `onStop` (the host ended it).

`sendInput` options: `loops` (`0` repeats until stopped), `speed` (divides every recorded delay, clamped 0–100, default `1`), `onLoop(n)` (loops finished, throttled), `onDone` (a finite run ending on its own — a stop you asked for does not call it).

```js
const events = [];
const recorder = await pluginApi.readInput(ev => events.push(ev));
await recorder.stop();

const playback = await pluginApi.sendInput(events, { loops: 2, speed: 1 });
await playback.pause();   // held keys are released, then pressed again on resume
await playback.resume();
await playback.stop();
```

### OAuth (needs `oauth`)

| Call | Does |
|---|---|
| `login(provider)` | Opens the system browser for sign-in (PKCE). Up to 5 min. |
| `logout(provider)` | Drops the stored tokens. |
| `isLoggedIn(provider)` | `true` / `false`. |
| `request(url, { auth: provider })` | The host attaches the Bearer token for you. |

## 8. Permissions

Declare in `manifest.json`. The user sees this list in the approval dialog when enabling your plugin. Names are case-insensitive.

| String | Grants |
|---|---|
| `network` | Outbound HTTP for `request()` in the UI |
| `storage` | Persistent storage — `getStore`/`setStore` in the UI, `GetStoragePath()` in C# |
| `system` | CPU / RAM info |
| `audio` | Read and set system volume |
| `media` | Now-playing info and transport control |
| `microphone` | Microphone mute state and toggle |
| `bluetooth` | Enumerate paired Bluetooth devices |
| `hid` | Enumerate USB/HID peripherals |
| `local-network` | Lets `request()` reach localhost and the private LAN (local companion apps, Ollama, Home Assistant) |
| `oauth` | Host-mediated sign-in to external accounts |
| `input-read` | Watch every keystroke and mouse action (`readInput`) |
| `input-send` | Synthesise keystrokes and mouse actions (`sendInput`) |

Declare only what you use — the user reads this list before enabling your plugin.

`saveFile`, `openFile` and `captureHotkey` are not in this table: they need no permission.

## 9. OAuth provider config (advanced)

With the `oauth` permission, declare providers in the manifest:

```json
"oauth": {
  "spotify": {
    "authorizeUrl": "https://accounts.spotify.com/authorize",
    "tokenUrl":     "https://accounts.spotify.com/api/token",
    "clientId":     "your-public-client-id",
    "scopes":       ["user-read-playback-state"],
    "apiHosts":     ["api.spotify.com"],
    "redirectUri":  "http://127.0.0.1:43210/callback"
  }
}
```

`apiHosts` is the allowlist for `request({ auth })`.

## 10. UI guidelines

- Design small and responsive. Your tile shares the bar with up to two other plugins, so its width shrinks as more are added. Use `clamp()` font sizes and a flexible layout.
- Tile viewport sizes on a 1920 x 1080 monitor at 100% scale (everything scales proportionally with monitor resolution and DPI, so treat these as ratios, not fixed values):

| Tiles on the bar | Layout (`data-layout`) | Your viewport |
|---|---|---|
| 1 | `full` (100%) | 600 x 150 px |
| 2 | `half` (50%) | 300 x 150 px |
| 3 | `third` (33%) | 200 x 150 px |

The host sets the current layout as `data-layout` on `<html>` and fires a `layoutchange` window event whenever it changes, so your CSS can adapt: `:root[data-layout="third"] .meta { display: none; }`
- Bundle CSS, JS, and images inside `ui/`. Fetch binary data through `request({ responseType: 'base64' })`.
- **JS timers are throttled while the overlay is collapsed.** Your tile is a Chromium page: once hidden, `setInterval` runs about once per second, and after five minutes hidden about once per minute, whatever interval you set. Shortening the interval does not help.

  Derive elapsed time from the clock rather than counting ticks, so a late tick still shows the right value:

  ```js
  // loses time whenever a tick is deferred
  setInterval(() => { seconds += 1; }, 1000);
  // a late tick jumps to the correct value
  setInterval(() => { render(Date.now() - startedAt); }, 1000);
  ```

  To show progress while collapsed, use `setOverlayStatus()` instead of redrawing the tile.

## 11. Test locally and publish

### Local test loop

1. Turn ON **Developer Mode** in Dynamic Overlay settings.
2. `dotnet build -c Release`, then copy the `.dop` into `%LocalAppData%\DynamicOverlay\Plugins\`.
3. Reload the overlay. The plugin appears. Replacing the `.dop` with a newer build re-extracts on the next reload.
4. Enable it. If you declared permissions, the approval dialog lists them.

### Publish to Steam Workshop

Upload through the Dynamic Overlay app (Upload dialog). It validates your plugin and publishes it, and you set the title, description, icon, and screenshots there. Subscribers receive updates automatically through Steam.
