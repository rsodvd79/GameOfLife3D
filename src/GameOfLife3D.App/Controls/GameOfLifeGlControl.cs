#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using GameOfLife3D.App.ViewModels;

namespace GameOfLife3D.App.Controls;

public class GameOfLifeGlControl : Control
    {
        private float _theta = 0.5f;
        private float _phi = 1.1f;
        private float _radius = 50f;
        private Vector3 _target;

        private bool _isDragging;
        private Point _lastMousePos;
        private MainViewModel? _viewModel;
        private int _lastGridSize = -1;
        private float _time = 0f;
        private (Vector3 pos, float brightness)[]? _stars;

        public GameOfLifeGlControl()
        {
            ClipToBounds = true;
        }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        DataContextChanged += OnDataContextChanged;
        if (DataContext is MainViewModel vm) AttachViewModel(vm);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DataContextChanged -= OnDataContextChanged;
        if (_viewModel != null)
            _viewModel.SimulationStepped -= OnSimulationStepped;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
            _viewModel.SimulationStepped -= OnSimulationStepped;
        if (DataContext is MainViewModel vm) AttachViewModel(vm);
    }

    private void AttachViewModel(MainViewModel vm)
    {
        _viewModel = vm;
        vm.SimulationStepped += OnSimulationStepped;
        UpdateCameraForGrid(vm.GridSize);
    }

    private void OnSimulationStepped(object? sender, EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(InvalidateVisual);
    }

    private void UpdateCameraForGrid(int gridSize)
    {
        _target = new Vector3(gridSize / 2f, gridSize / 2f, gridSize / 2f);
        // Reset radius only when the grid size actually changes, not on every render frame.
        if (gridSize == _lastGridSize) return;
        _radius = gridSize * 2.2f;
        _lastGridSize = gridSize;
        GenerateStars(gridSize);
    }

    private void GenerateStars(int gridSize)
    {
        var rand = new Random(gridSize * 12345); // deterministic per grid size
        int count = Math.Clamp(gridSize * 2, 100, 500);
        float radius = gridSize * 3f;
        _stars = new (Vector3 pos, float brightness)[count];
        for (int i = 0; i < count; i++)
        {
            // Uniform distribution on sphere
            float u = (float)rand.NextDouble();
            float v = (float)rand.NextDouble();
            float theta = 2f * MathF.PI * u;
            float phi = MathF.Acos(2f * v - 1f);
            float r = radius * MathF.Pow((float)rand.NextDouble(), 1f / 3f);
            var pos = new Vector3(
                r * MathF.Sin(phi) * MathF.Cos(theta),
                r * MathF.Cos(phi),
                r * MathF.Sin(phi) * MathF.Sin(theta)
            ) + _target;
            _stars[i] = (pos, 0.3f + 0.7f * (float)rand.NextDouble());
        }
    }

    public void ZoomIn()
    {
        _radius = Math.Clamp(_radius * 0.8f, 5f, 500f);
        InvalidateVisual();
    }

    public void ZoomOut()
    {
        _radius = Math.Clamp(_radius * 1.25f, 5f, 500f);
        InvalidateVisual();
    }

    public void ResetZoom()
    {
        if (_viewModel == null) return;
        _radius = _viewModel.GridSize * 2.2f;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        if (_viewModel == null)
        {
            context.FillRectangle(new SolidColorBrush(Color.Parse("#1a1a2e")), bounds);
            return;
        }

        UpdateCameraForGrid(_viewModel.GridSize);
        _time += 0.016f; // ~60fps time accumulator

        var cells = new List<(int x, int y, int z)>();
        foreach (var cell in _viewModel.Engine.Grid.GetLiveCells())
            cells.Add(cell);

        context.Custom(new GameOfLifeRenderOp(bounds, cells, _theta, _phi, _radius, _target, _time, _stars));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        _isDragging = true;
        e.Pointer.Capture(this);
        _lastMousePos = e.GetPosition(this);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
        if (e.Pointer.Captured == this)
            e.Pointer.Capture(null);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging) return;
        var pos = e.GetPosition(this);
        float dx = (float)(pos.X - _lastMousePos.X);
        float dy = (float)(pos.Y - _lastMousePos.Y);
        _theta -= dx * 0.01f;
        _phi   -= dy * 0.01f;
        _phi    = Math.Clamp(_phi, 0.1f, MathF.PI - 0.1f);
        _lastMousePos = pos;
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        _radius -= (float)e.Delta.Y * _radius * 0.1f;
        _radius  = Math.Clamp(_radius, 5f, 500f);
        InvalidateVisual();
    }
}

internal sealed class GameOfLifeRenderOp : ICustomDrawOperation
{
    private readonly Rect _bounds;
    private readonly List<(int x, int y, int z)> _cells;
    private readonly float _theta, _phi, _radius;
    private readonly Vector3 _target;
    private readonly float _time;
    private readonly (Vector3 pos, float brightness)[]? _stars;

    public GameOfLifeRenderOp(Rect bounds, List<(int, int, int)> cells,
        float theta, float phi, float radius, Vector3 target,
        float time, (Vector3 pos, float brightness)[]? stars)
    {
        _bounds = bounds;
        _cells  = cells;
        _theta  = theta;
        _phi    = phi;
        _radius = radius;
        _target = target;
        _time   = time;
        _stars  = stars;
    }

    public Rect Bounds => _bounds;
    public bool HitTest(Point p) => _bounds.Contains(p);

    public bool Equals(ICustomDrawOperation? other)
    {
        if (other is not GameOfLifeRenderOp o) return false;
        if (_bounds != o._bounds) return false;
        if (_theta != o._theta || _phi != o._phi || _radius != o._radius) return false;
        if (_target != o._target) return false;
        if (_cells.Count != o._cells.Count) return false;
        for (int i = 0; i < _cells.Count; i++)
            if (_cells[i] != o._cells[i]) return false;
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as ICustomDrawOperation);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_bounds);
        hash.Add(_theta);
        hash.Add(_phi);
        hash.Add(_radius);
        hash.Add(_target);
        hash.Add(_cells.Count);
        return hash.ToHashCode();
    }

    public void Dispose() { }

    public void Render(ImmediateDrawingContext context)
    {
        var lease = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
        if (lease == null) return;
        using var l = lease.Lease();
        var canvas = l.SkCanvas;

        float w = (float)_bounds.Width;
        float h = (float)_bounds.Height;
        if (w <= 0 || h <= 0) return;

        canvas.Clear(new SKColor(26, 26, 46));

        // Build view-projection matrix (System.Numerics row-major)
        var eye = new Vector3(
            _radius * MathF.Sin(_phi) * MathF.Cos(_theta),
            _radius * MathF.Cos(_phi),
            _radius * MathF.Sin(_phi) * MathF.Sin(_theta)
        ) + _target;

        var view = Matrix4x4.CreateLookAt(eye, _target, Vector3.UnitY);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, w / h, 0.1f, 2000f);
        var vp   = view * proj;

        float sizeBase = Math.Min(w, h);
        float maxDepth = 1.5f;

        if (_stars != null && _stars.Length > 0)
        {
            using var starPaint = new SKPaint { IsAntialias = false };
            foreach (var (pos, bright) in _stars)
            {
                var wpos = new Vector4(pos, 1f);
                var clip = Vector4.Transform(wpos, vp);
                if (clip.W <= 0f) continue;
                float invW = 1f / clip.W;
                float sx = (clip.X * invW * 0.5f + 0.5f) * w;
                float sy = (1f - (clip.Y * invW * 0.5f + 0.5f)) * h;
                if (sx < 0 || sx > w || sy < 0 || sy > h) continue;
                byte alpha = (byte)(bright * 180);
                starPaint.Color = new SKColor(200, 210, 255, alpha);
                canvas.DrawPoint(sx, sy, starPaint);
            }
        }

        // Early out if no cells
        if (_cells.Count == 0) return;

        // Project all cells; keep a positional lookup for tube detection.
        var projData = new (float sx, float sy, float size, float depth)[_cells.Count];
        var projValid = new bool[_cells.Count];
        var cellIndex = new Dictionary<(int, int, int), int>(_cells.Count);

        for (int i = 0; i < _cells.Count; i++)
        {
            var (cx, cy, cz) = _cells[i];
            cellIndex[(cx, cy, cz)] = i;

            var world = new Vector4(cx + 0.5f, cy + 0.5f, cz + 0.5f, 1f);
            var clip  = Vector4.Transform(world, vp);
            if (clip.W <= 0f) continue;

            float invW  = 1f / clip.W;
            float depth = clip.Z * invW;
            float sx    = (clip.X * invW * 0.5f + 0.5f) * w;
            float sy    = (1f - (clip.Y * invW * 0.5f + 0.5f)) * h;
            float size  = Math.Clamp(sizeBase * invW * 0.55f, 2f, 60f);

            projData[i]  = (sx, sy, size, depth);
            projValid[i] = true;
        }

        // ── Build tube list (orthogonal neighbours only, +x/+y/+z to skip duplicates) ──
        var tubes = new List<(float sx1, float sy1, float sx2, float sy2, float w1, float w2, float depth, float d1, float d2)>();

        for (int i = 0; i < _cells.Count; i++)
        {
            if (!projValid[i]) continue;
            var (cx, cy, cz) = _cells[i];
            var (sx1, sy1, s1, d1) = projData[i];

            if (cellIndex.TryGetValue((cx + 1, cy,     cz),     out int jx) && projValid[jx])
            {
                var (sx2, sy2, s2, d2) = projData[jx];
                tubes.Add((sx1, sy1, sx2, sy2, s1, s2, (d1 + d2) * 0.5f, d1, d2));
            }
            if (cellIndex.TryGetValue((cx,     cy + 1, cz),     out int jy) && projValid[jy])
            {
                var (sx2, sy2, s2, d2) = projData[jy];
                tubes.Add((sx1, sy1, sx2, sy2, s1, s2, (d1 + d2) * 0.5f, d1, d2));
            }
            if (cellIndex.TryGetValue((cx,     cy,     cz + 1), out int jz) && projValid[jz])
            {
                var (sx2, sy2, s2, d2) = projData[jz];
                tubes.Add((sx1, sy1, sx2, sy2, s1, s2, (d1 + d2) * 0.5f, d1, d2));
            }
        }

        // ── Painter's algorithm: back-to-front ──
        var drawOrder = new List<(float depth, bool isCell, int index)>(tubes.Count + _cells.Count);
        for (int t = 0; t < tubes.Count; t++)
            drawOrder.Add((tubes[t].depth, false, t));
        for (int i = 0; i < _cells.Count; i++)
            if (projValid[i]) drawOrder.Add((projData[i].depth, true, i));
        drawOrder.Sort(static (a, b) => b.depth.CompareTo(a.depth));

        // Render scene to offscreen surface for bloom compositing
        using var mainSurface = SKSurface.Create(new SKImageInfo((int)w, (int)h, SKColorType.Rgba8888));
        var mainCanvas = mainSurface.Canvas;
        mainCanvas.Clear(new SKColor(26, 26, 46, 0));

        using var glowPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var borderPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke };
        using var tubeFillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round };

        foreach (var (depth, isCell, index) in drawOrder)
        {
            if (isCell)
            {
                var (sx, sy, size, d) = projData[index];
                var (cx, cy, cz) = _cells[index];
                float bright = Math.Clamp(1.15f - d * 0.5f, 0.3f, 1f);
                float fog = Math.Clamp(1f - (d + 0.5f) / (maxDepth + 0.5f), 0.15f, 1f);
                float alphaFog = fog * 255f;
                float r = size * 0.5f;

                // Dynamic hue based on Z-layer and time
                float hue = (cz * 0.04f + _time * 0.015f) % 1f;
                float saturation = 75f;
                float lightness = 45f + 25f * bright;

                var cellColor = SKColor.FromHsl(hue * 360f, saturation, lightness, (byte)Math.Clamp(alphaFog, 0, 255));
                var glowColor = SKColor.FromHsl(hue * 360f, saturation, lightness, (byte)Math.Clamp(alphaFog * 0.2f, 0, 255));
                var borderColor = SKColor.FromHsl(hue * 360f, saturation, lightness + 15f, (byte)Math.Clamp(alphaFog * 0.5f, 0, 255));

                // Glow layer
                glowPaint.Color = glowColor;
                mainCanvas.DrawCircle(sx, sy, r * 2.5f, glowPaint);

                // Fill
                fillPaint.Color = cellColor;
                mainCanvas.DrawCircle(sx, sy, r, fillPaint);

                // Thin border
                borderPaint.Color = borderColor;
                borderPaint.StrokeWidth = Math.Max(1f, r * 0.15f);
                mainCanvas.DrawCircle(sx, sy, r - borderPaint.StrokeWidth * 0.5f, borderPaint);
            }
            else
            {
                var (sx1, sy1, sx2, sy2, w1, w2, d, d1, d2) = tubes[index];
                float bright = Math.Clamp(1.15f - d * 0.5f, 0.25f, 1f);
                float fog = Math.Clamp(1f - (d + 0.5f) / (maxDepth + 0.5f), 0.1f, 1f);
                float alphaFog = fog * 255f;
                float avgZ = d1 + d2;
                float hue = (avgZ * 0.02f + _time * 0.015f) % 1f;
                float lightness = 40f + 20f * bright;
                var tubeColor = SKColor.FromHsl(hue * 360f, 60f, lightness, (byte)Math.Clamp(alphaFog, 0, 255));

                tubeFillPaint.StrokeWidth = Math.Min(w1, w2) * 0.5f;
                tubeFillPaint.Color = tubeColor;
                mainCanvas.DrawLine(sx1, sy1, sx2, sy2, tubeFillPaint);
            }
        }

        // Composite: draw scene, then blur overlay for bloom
        using var mainImage = mainSurface.Snapshot();
        canvas.DrawImage(mainImage, 0, 0);

        using var bloomPaint = new SKPaint
        {
            ImageFilter = SKImageFilter.CreateBlur(6f, 6f, SKShaderTileMode.Clamp),
            BlendMode = SKBlendMode.Screen,
            Color = new SKColor(255, 255, 255, 50)
        };
        canvas.DrawImage(mainImage, 0, 0, bloomPaint);

        // ── Axis indicator ──
        float axisLen = 40f;
        float originX = 60f;
        float originY = h - 60f;

        // Project grid center to screen as reference
        var centerWorld = new Vector4(_target, 1f);
        var centerClip = Vector4.Transform(centerWorld, vp);
        float centerScrX = 0f, centerScrY = 0f;
        bool centerValid = centerClip.W > 0f;
        if (centerValid)
        {
            float invW = 1f / centerClip.W;
            centerScrX = (centerClip.X * invW * 0.5f + 0.5f) * w;
            centerScrY = (1f - (centerClip.Y * invW * 0.5f + 0.5f)) * h;
        }

        var axisVectors = new Vector3[]
        {
            new Vector3(1, 0, 0), // X
            new Vector3(0, 1, 0), // Y
            new Vector3(0, 0, 1)  // Z
        };

        var axisColors = new SKColor[]
        {
            new SKColor(255, 80, 80, 220),   // X - red
            new SKColor(80, 255, 80, 220),   // Y - green
            new SKColor(80, 160, 255, 220)   // Z - blue
        };

        for (int i = 0; i < 3; i++)
        {
            // Project axis endpoint in world space
            var axisWorld = new Vector4(_target + axisVectors[i] * 3f, 1f);
            var axisClip = Vector4.Transform(axisWorld, vp);
            if (axisClip.W <= 0f || !centerValid) continue;

            float invW = 1f / axisClip.W;
            float ax = (axisClip.X * invW * 0.5f + 0.5f) * w;
            float ay = (1f - (axisClip.Y * invW * 0.5f + 0.5f)) * h;

            // Screen-space direction from projected center to projected axis tip
            float dx = ax - centerScrX;
            float dy = ay - centerScrY;
            float len = MathF.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) continue;

            float scale = axisLen / len;
            float endX = originX + dx * scale;
            float endY = originY + dy * scale;

            using var axisPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3f,
                Color = axisColors[i],
                StrokeCap = SKStrokeCap.Round
            };
            canvas.DrawLine(originX, originY, endX, endY, axisPaint);

            // Arrow tip
            float tipSize = 8f;
            float nx = -dy / len;
            float ny = dx / len;
            using var tipPath = new SKPath();
            tipPath.MoveTo(endX, endY);
            tipPath.LineTo(endX - dx / len * tipSize - nx * tipSize * 0.5f, endY - dy / len * tipSize - ny * tipSize * 0.5f);
            tipPath.LineTo(endX - dx / len * tipSize + nx * tipSize * 0.5f, endY - dy / len * tipSize + ny * tipSize * 0.5f);
            tipPath.Close();
            using var tipPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = axisColors[i]
            };
            canvas.DrawPath(tipPath, tipPaint);
        }

        // Labels
        string[] labels = { "X", "Y", "Z" };
        for (int i = 0; i < 3; i++)
        {
            var axisWorld = new Vector4(_target + axisVectors[i] * 3.5f, 1f);
            var axisClip = Vector4.Transform(axisWorld, vp);
            if (axisClip.W <= 0f || !centerValid) continue;

            float invW = 1f / axisClip.W;
            float ax = (axisClip.X * invW * 0.5f + 0.5f) * w;
            float ay = (1f - (axisClip.Y * invW * 0.5f + 0.5f)) * h;

            float dx = ax - centerScrX;
            float dy = ay - centerScrY;
            float len = MathF.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) continue;

            float scale = (axisLen + 14f) / len;
            float lx = originX + dx * scale - 5f;
            float ly = originY + dy * scale + 5f;

            using var labelPaint = new SKPaint
            {
                IsAntialias = true,
                Color = axisColors[i],
                TextSize = 13f,
                TextAlign = SKTextAlign.Center
            };
            canvas.DrawText(labels[i], lx, ly, labelPaint);
        }
    }
}

