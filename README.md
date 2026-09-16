# SPT-UIScale

BepInEx client plugin for SPT 4.1.x that unlocks UI scaling for the inventory, trader, and task screens. It has been checked against the SPT 4.1.5 client and adjusts panel layouts so the stash, gear, and task-sort controls fit the screen at higher resolutions.

## Features

- Configurable UI scale as a percentage of vanilla (50–150%)
- Automatically adjusts when changing resolution in-game
- Inventory screen: gear panel expands to fill available space, stash anchored to the right
- Trader screen: trader items anchored left, stash anchored right, deal panel centered
- Tasks screen: sort headers track the rendered quest-list columns
- Works with any resolution (1440p, 4K, ultrawide, etc.)

## Installation

1. Download the latest release ZIP
2. Extract into your SPT installation directory — the DLL goes to `BepInEx/plugins/`
3. Launch SPT

## Configuration

After first launch, edit `BepInEx/config/com.vonbraunz.uiscale.cfg`:

| Setting | Default | Description |
|---------|---------|-------------|
| **Enabled** | `true` | Toggle the mod on/off without uninstalling |
| **Scale Percent** | `100` | UI scale as a percentage of vanilla. `100` = no change, `75` = 75% size (more grid space), `50` = half size. Range: 50–150 |
| **Align Sort Header** | `true` | Align Tasks screen sort headers with their rendered columns when UI scaling is active |
| **Log Canvas Names** | `false` | Debug logging to BepInEx console |

### Recommended values

| Resolution | Scale Percent | Effect |
|------------|--------------|--------|
| 1080p | 100 | No change (vanilla) |
| 1440p | 75–85 | More inventory/stash space |
| 4K | 50–75 | Significantly more grid space |

## How It Works

EFT uses a central UI scale manager that forces all canvases to a 1080p reference resolution via `ConstantPixelSize` scaling. Every frame it calculates `Min(screenWidth/1920, screenHeight/1080)` and applies that to all registered `CanvasScaler` components. The controller is obfuscated differently between client builds, so the plugin locates it from its stable scaler-registration methods instead of hardcoding its generated class name.

This mod patches that pipeline:

1. **CanvasScalerPatch** — intercepts the game's scaler-application method and multiplies the auto-calculated scale factor by your configured percentage
2. **InventoryStretchPatch** — hooks `InventoryScreen.Show()` to reanchor the gear and stash panels so they fill the wider canvas
3. **TraderStretchPatch** — hooks `TraderScreensGroup.Show()` to reanchor the trader items and stash panels
4. **TaskSortAlignmentPatch** — copies the task-list column layout to the Tasks sort header once the task list is created

## Building

Requires an SPT 4.1.x client (tested with 4.1.5). Set `TarkovDir` to the client directory when building, or override it in the `.csproj`.

```
dotnet build Client/UIScale.Client.csproj -c Release
```

Output: `Client/bin/Release/UIScale.Client.dll` and `Client/release/UIScale.zip`

## Compatibility

- SPT 4.1.x (validated against 4.1.5)
- BepInEx 5.x
- No server-side component required
