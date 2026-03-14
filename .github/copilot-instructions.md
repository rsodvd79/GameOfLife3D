# GameOfLife3D – Copilot Instructions

## Build & Run

```bash
# Build the entire solution
dotnet build

# Run the application
dotnet run --project src/GameOfLife3D.App

# Build release
dotnet build -c Release
```

## Architecture Overview

The solution is split into two projects:

### `GameOfLife3D.Core` (net8.0 – no UI dependencies)
Pure simulation logic, fully unit-testable in isolation.

| File | Responsibility |
|------|---------------|
| `Grid3D.cs` | Double-buffered 3D boolean grid with toroidal (wrapping) Moore neighborhood |
| `SimulationEngine.cs` | Drives `Step()`, `Randomize(density)`, `Clear()` |
| `Rules/IRule3D.cs` | Interface: `bool NextState(bool current, int neighbors)` |
| `Rules/StandardRule3D.cs` | Default "445" rule: survive on {5,6,7}, born on {6} |

**Double-buffer pattern**: `Grid3D` holds two `bool[,,]` arrays (`_front` / `_back`).
`SimulationEngine.Step()` reads from `_front` (via `GetFront`) and writes to `_back` (via `SetBack`),
then calls `Grid3D.Swap()` which atomically swaps the references under a `lock`. This lets the GL
render thread read `_front` safely at any time.

### `GameOfLife3D.App` (net8.0 – Avalonia 11, CommunityToolkit.Mvvm)

| File | Responsibility |
|------|---------------|
| `ViewModels/MainViewModel.cs` | Observable state + commands; `System.Timers.Timer` drives simulation; fires `SimulationStepped` event |
| `Controls/GameOfLifeGlControl.cs` | OpenGL rendering via `OpenGlControlBase`; instanced cube rendering |
| `Views/MainWindow.axaml` | DockPanel layout: toolbar (top), settings (bottom), GL viewport (center) |

## Key Conventions

### Rule Configuration
Rules live in `GameOfLife3D.Core.Rules`. To add a new rule:
1. Create a class implementing `IRule3D`.
2. Assign it to `SimulationEngine.Rule` on the ViewModel.
3. Optionally expose it in the UI via `MainViewModel`.

The built-in `StandardRule3D` exposes `int[] SurvivalCounts` and `int[] BirthCounts` as mutable
arrays, which the ViewModel updates when the user edits the text boxes.

### Camera System (`GameOfLifeGlControl`)
- Spherical coordinates: `_radius`, `_theta` (azimuth), `_phi` (elevation)
- Eye position = `(r·sin φ·cos θ, r·cos φ, r·sin φ·sin θ) + target`
- Target = grid centre `(SizeX/2, SizeY/2, SizeZ/2)`
- Mouse drag → δtheta / δphi; scroll wheel → radius zoom
- `System.Numerics.Matrix4x4` for view/projection; matrices uploaded transposed (`GL_TRUE`)

### Renderer (`GameOfLifeGlControl`)
The 3D view uses Avalonia's `ICustomDrawOperation` + SkiaSharp rather than OpenGL (OpenGL is deprecated on macOS and crashes with Avalonia's Metal bridge). Rendering happens in `GameOfLifeRenderOp.Render(ImmediateDrawingContext)`:
- Perspective projection via `System.Numerics.Matrix4x4` (row-major; `Vector4.Transform(v, M)` = `v * M`)
- Live cells projected to screen space, sorted back-to-front (painter's algorithm), drawn as depth-shaded circles on an `SKCanvas`
- `InvalidateVisual()` is called on the UI thread whenever `SimulationStepped` fires

### Threading
- `System.Timers.Timer` fires on a thread-pool thread.
- `SimulationEngine.Step()` is called directly from the timer callback (thread-safe via the Grid lock).
- UI property updates (`Generation`, `LiveCellCount`) are posted to `Dispatcher.UIThread.Post`.
- `SimulationStepped` event triggers `RequestNextFrameRendering()` which is safe from any thread.
