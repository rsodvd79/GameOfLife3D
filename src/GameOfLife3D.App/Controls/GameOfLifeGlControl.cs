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

        // Snapshot live cells on the UI thread before passing to render op
        var cells = new List<(int x, int y, int z)>();
        foreach (var cell in _viewModel.Engine.Grid.GetLiveCells())
            cells.Add(cell);

        context.Custom(new GameOfLifeRenderOp(bounds, cells, _theta, _phi, _radius, _target));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isDragging = true;
        _lastMousePos = e.GetPosition(this);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
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

    public GameOfLifeRenderOp(Rect bounds, List<(int, int, int)> cells,
        float theta, float phi, float radius, Vector3 target)
    {
        _bounds = bounds;
        _cells  = cells;
        _theta  = theta;
        _phi    = phi;
        _radius = radius;
        _target = target;
    }

    public Rect Bounds => _bounds;
    public bool HitTest(Point p) => _bounds.Contains(p);
    public bool Equals(ICustomDrawOperation? other) => false;
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

        canvas.Clear(new SKColor(26, 26, 46)); // #1a1a2e

        if (_cells.Count == 0) return;

        // Build view-projection matrix (System.Numerics row-major)
        var eye = new Vector3(
            _radius * MathF.Sin(_phi) * MathF.Cos(_theta),
            _radius * MathF.Cos(_phi),
            _radius * MathF.Sin(_phi) * MathF.Sin(_theta)
        ) + _target;

        var view = Matrix4x4.CreateLookAt(eye, _target, Vector3.UnitY);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, w / h, 0.1f, 2000f);
        var vp   = view * proj;

        // Project all cells to screen space
        var projected = new List<(float sx, float sy, float size, float depth)>(_cells.Count);
        float sizeBase = Math.Min(w, h);

        foreach (var (cx, cy, cz) in _cells)
        {
            var world = new Vector4(cx + 0.5f, cy + 0.5f, cz + 0.5f, 1f);
            var clip  = Vector4.Transform(world, vp);
            if (clip.W <= 0f) continue;

            float invW = 1f / clip.W;
            float ndcX = clip.X * invW;
            float ndcY = clip.Y * invW;
            float depth = clip.Z * invW;

            // NDC → screen (Y is flipped: +Y up in NDC, +Y down on screen)
            float sx = (ndcX  * 0.5f + 0.5f) * w;
            float sy = (1f - (ndcY * 0.5f + 0.5f)) * h;

            // Perspective-correct radius: 1 world unit at this depth
            float size = Math.Clamp(sizeBase * invW * 0.55f, 2f, 60f);
            projected.Add((sx, sy, size, depth));
        }

        // Painter's algorithm: back-to-front
        projected.Sort(static (a, b) => b.depth.CompareTo(a.depth));

        // Draw as circles with depth shading
        using var fillPaint   = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var strokePaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 0.7f };

        foreach (var (sx, sy, size, depth) in projected)
        {
            float bright = Math.Clamp(1.15f - depth * 0.5f, 0.3f, 1f);
            byte g = (byte)(212 * bright);
            byte b = (byte)(170 * bright);
            float r = size * 0.5f;

            fillPaint.Color   = new SKColor(0, g, b, 210);
            strokePaint.Color = new SKColor(0, 255, 210, 70);

            canvas.DrawCircle(sx, sy, r, fillPaint);
            canvas.DrawCircle(sx, sy, r, strokePaint);
        }
    }
}

