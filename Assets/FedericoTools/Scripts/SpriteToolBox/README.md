# Sprite Toolbox

Sprite Toolbox is a pixel-art and frame-animation editor integrated into the Unity Editor. Documents, editing operations, and command history live in the Core assembly; the IMGUI window is responsible only for presentation, input, and Editor-specific workflows.

## Requirements

- Unity 6000.3 or compatible. This version was verified with Unity `6000.3.23f1`.
- The `com.unity.2d.sprite` package, required for sprite-sheet slicing and export.
- Unity Test Framework, to run the EditMode test suite.

## Open the tool

Open `Tools > Sprite Toolbox > Pixel Editor`.

The window automatically attempts to reopen the last valid `SpriteDocumentAsset`. Alternatively, use `File > Open…` to open a document `.asset` or import a `.png` image. Recent documents are available through `File > Open Recent`.

## Interface

The window has four resizable areas:

- **Palette**, on the left: primary and secondary colours, swatches, recent colours, presets, and harmonic or planetary generation.
- **Canvas**, in the centre: viewport, zoom, pan, grid, compositing, and onion skin.
- **Tools**, on the right: tools, brush size, symmetry, tiled mode, pixel-perfect mode, selection, and recolour.
- **Timeline**, at the bottom: frames, layers, visibility, locks, playback, tags, duration, and onion skin.

Drag the separators between panels to resize them. In the canvas, the mouse wheel changes zoom, `Ctrl/Cmd + mouse wheel` changes brush size, and the middle mouse button pans.

## Tools

- **Pencil**: continuous drawing with a configurable brush.
- **Eraser**: clears pixels.
- **Fill**: fills connected areas.
- **Line**: draws a line with a preview; endpoints can be outside the canvas and are clipped to the document.
- **Eyedropper**: samples the visible composited colour.
- **Select**: creates and moves a rectangular selection; supports copy, cut, paste, fill, clear, and flip.
- **Recolor**: lists colours from the selected layer as locked sources, ordered by related tones, and lets you preview a destination colour for each one. It shows the source-colour count and can reduce it to a smaller target through perceptual, frequency-weighted colour families; every proposed target remains editable. Sources refresh when the selected layer or frame changes. Preview defaults on up to 512 px on the longest side and can always be toggled manually. **Apply** commits every replacement as one undoable edit and refreshes the source list. **From Canvas** uses the same tonal ordering.

Right-click uses the secondary colour on compatible tools. Pencil strokes support brush size, horizontal and vertical symmetry, tiled mode, and pixel-perfect mode.

## Shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl/Cmd + N` | Create a new document |
| `Ctrl/Cmd + O` | Open a document or PNG image |
| `Ctrl/Cmd + W` | Close Sprite Toolbox safely |
| `Ctrl/Cmd + Q` | Exit Sprite Toolbox safely |
| `B` | Select Pencil |
| `X` | Swap primary and secondary colours |
| `Esc` | Clear the selection without clearing pixels |
| `Ctrl/Cmd + A` | Select all pixels |
| `Ctrl/Cmd + I` | Invert the selection |
| `Ctrl/Cmd + Z` | Undo |
| `Ctrl/Cmd + Y` | Redo |
| `Ctrl/Cmd + C` | Copy the selection |
| `Ctrl/Cmd + X` | Cut the selection |
| `Ctrl/Cmd + Shift + X` | Crop the canvas to the selection |
| `Ctrl/Cmd + V` | Paste the selection |

Shortcuts are not intercepted while a text field has focus, except `B`, which always selects Pencil, and `X`, which swaps the active colors.

## Palette

Click a swatch to set the primary colour; right-click to set the secondary colour. `Swap` exchanges the two colours and `Transparent` sets the primary colour to transparent.

Save a palette as a `SpritePaletteAsset`, load it from project presets, build one from the visible colours of the current canvas frame, or generate one by choosing a harmony and colour count. The `Planetary` mode exposes a seed, base hue, variation, saturation range, and value range.

The distributed presets are `Nord16` (default), `Solarized16`, `PICO8`, and `Grayscale16`. Nord and Solarized use the MIT licence; PICO-8 is CC0. Sources, attributions, and licence texts are included in `Third-Party Notices.txt` and `Licenses`.

## Timeline and onion skin

The timeline lets you:

- select, add, duplicate, and remove frames;
- add, duplicate, reorder, merge, hide, or lock layers;
- create and edit animation tags, including name, range, direction, and colour;
- change frame duration through **Properties…** or double-clicking its number;
- play the animation and navigate to the first, previous, next, or last frame;
- show onion skin to the left and right.

With **Loop Onion** enabled, the first frame uses the last frame as its left onion skin and the last frame uses the first frame as its right onion skin. The onion cache is invalidated when an adjacent frame changes.

## Canvas commands

The **Sprite** menu includes:

- **Resize Canvas**: preserves pixels from the top-left origin, crops pixels outside smaller bounds, and fills extended space with transparent pixels.
- **Flip Horizontal** and **Flip Vertical**: flip every cel in every frame and layer.

All three operations are one undoable document edit.

## Save, import, and export

The project format is `SpriteDocumentAsset`. `File > Save Project As…` creates the asset; `Save Project` updates the associated asset. An asterisk in the title indicates unsaved changes. Closing the tool offers to save; choosing Save updates the current asset or opens the destination picker for an unsaved document.

When importing a project PNG, Sprite Toolbox temporarily enables Read/Write and Point filtering, then restores both importer settings when the tool closes or opens another document.

Cel pixels are stored as `Color32`, using four bytes per pixel.

Available workflows:

- import PNG files from inside or outside the Unity project;
- use `File > Export…` to combine a project copy, current-frame PNG, all-frame PNG files, a sprite sheet, and its optional `AnimationClip` in one export run;
- use `File > Quick Export` for direct current-frame PNG, all-frame PNG, or sprite-sheet export;
- export a sprite sheet with automatic slicing and its `SpriteSheetDescriptor`.

Temporary `isReadable` changes during import are restored by the import/export service.

## Architecture

```text
SpriteToolbox.Core
├── SpriteDocument and renderer
├── SpriteToolboxSession (editor-state facade)
├── document, pixel, stroke, selection, and palette application services
├── command history and operation result
└── ISpriteToolboxState / *Actions contracts

SpriteToolbox.Editor
├── SpriteToolboxWindow (composition and layout)
├── TimelinePanel / PalettePanel / ToolsPanel / CanvasPanel
├── CanvasController and CanvasRenderCache
└── persistence, import/export, and playback coordinator

SpriteToolbox.Editor.Tests
└── EditMode tests for domain, UI contracts, regressions, and serialization
```

Dependencies are one-way: Editor depends on Core; Core does not reference `UnityEditor`. Panels communicate with the session through read-only snapshots and action interfaces, without references to `SpriteToolboxWindow`.

## Verification

Verified on 12 September 2026:

- `SpriteToolbox.Core`, `SpriteToolbox.Editor`, and `SpriteToolbox.Editor.Tests`: 0 compile errors and 0 warnings;
- 44 EditMode tests are present; rerun the Unity Test Runner after the latest recolor changes;
- dedicated coverage exists for documents and tags, history, playback, stroke, pixel-perfect mode, coordinates and endpoints outside the canvas, selection move, tiled mode, symmetry, onion cache, panel contracts, and current-format serialization.

## Planned features

- Profiling and possible dirty-region or chunk rendering for large documents.
- Optional `EditorPrefs` persistence for tool preferences.
