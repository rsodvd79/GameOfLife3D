# GameOfLife3D – Agent Guide

## Build & Run

```bash
dotnet build
dotnet run --project src/GameOfLife3D.App
dotnet build -c Release
dotnet publish src/GameOfLife3D.App/GameOfLife3D.App.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o publish/osx-arm64
```

## Project Structure

Two projects under `src/`:

- **`GameOfLife3D.Core`** (`net8.0`) — pure simulation logic, no UI deps. Entrypoint: `SimulationEngine.Step()`
- **`GameOfLife3D.App`** (`net8.0`) — Avalonia 11 + CommunityToolkit.Mvvm + SkiaSharp. Entrypoint: `Program.cs`

## No Tests

There is no test project. No test runner, no test files anywhere. Do not look for or assume tests exist.

## Rendering: SkiaSharp, NOT OpenGL

The renderer uses `ICustomDrawOperation` + SkiaSharp (`SKCanvas`). The old copilot-instructions.md is wrong about OpenGL. Perspective projection via `System.Numerics.Matrix4x4`, painter's algorithm (back-to-front), cells drawn as depth-shaded circles, orthogonal neighbours connected by tubes.

## Thread Safety

`System.Timers.Timer` fires on a thread-pool thread → calls `Engine.Step()` directly. Grid uses a double-buffer (`_front`/`_back`), swap is lock-guarded. Render thread reads `_front` safely. UI updates posted via `Dispatcher.UIThread.Post`.

## Grid & Simulation

- Grid is **toroidal** (Moore neighbourhood wraps at edges via `% Size`)
- Size range: 10³–50³ (slider, snap-to-tick every 5)
- **90% birth cap** in `Step()` — prevents exponential fill crashes. New births suppressed when live cells ≥90% capacity.
- Default rule **"445"**: survive `{5,6,7}`, born `{6}`
- Rules editable at runtime via comma-separated text fields (e.g. `4,5`)

## Shape Placement

`Engine.PlaceShape()` is **additive** (union with existing cells). Does NOT clear the grid first. Position is random within bounds.

Shapes in `ShapeLibrary.cs` are verified against the "445" rule. Categories:
- 🔒 Still Life — mathematically stable
- 🌱 Seed — evolves to stable in 1–2 steps
- 🔁 Oscillator — cycles back after N steps
- 📐 Geometric — free-form, depends on active rule

## Camera

Spherical coordinates (`_radius`, `_theta`, `_phi`). Mouse drag rotates, scroll zooms. Keyboard `+`/`-` for zoom, `0` for reset. Radius resets to `gridSize * 2.2` only when grid size changes (zoom persists across renders).

## Notable Conventions

- `AvaloniaUseCompiledBindingsByDefault=true`, `x:DataType` required on Window
- `#nullable enable` throughout
- Dark theme (`RequestedThemeVariant="Dark"`)
- No linter/formatter config — only `dotnet build` for validation
- `AllowUnsafeBlocks=true` in App project (for macOS dock icon ObjC interop)
- macOS dock icon set via ObjC runtime (`MacDockIcon.cs`) — only active outside .app bundle
