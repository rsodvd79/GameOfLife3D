#nullable enable

using System;
using GameOfLife3D.Core.Rules;

namespace GameOfLife3D.Core;

public class SimulationEngine
{
    public Grid3D Grid { get; }

    private IRule3D _rule;
    public IRule3D Rule
    {
        get => _rule;
        set => _rule = value ?? throw new ArgumentNullException(nameof(value));
    }

    public SimulationEngine(Grid3D grid, IRule3D rule)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _rule = rule ?? throw new ArgumentNullException(nameof(rule));
    }

    public void Step()
    {
        int sx = Grid.SizeX, sy = Grid.SizeY, sz = Grid.SizeZ;

        // Work on a stable snapshot so concurrent UI writes to the front buffer
        // (randomize/clear/place) can't corrupt this step's reads.
        var front = Grid.SnapshotFront();

        // Cap births at 90 % of grid capacity to prevent exponential fill crashes.
        int maxLive = (int)(sx * sy * sz * 0.9);
        int currentLive = 0;
        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
        for (int z = 0; z < sz; z++)
            if (front[x, y, z]) currentLive++;

        bool birthAllowed = currentLive < maxLive;

        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
        for (int z = 0; z < sz; z++)
        {
            bool current   = front[x, y, z];
            int  neighbors = Grid.CountNeighbors(front, x, y, z);
            bool next      = Rule.NextState(current, neighbors);
            // When at capacity suppress new births; existing cells still live/die normally.
            if (!current && !birthAllowed) next = false;
            Grid.SetBack(x, y, z, next);
        }
        Grid.Swap();
    }

    public void Randomize(double density)
    {
        int sx = Grid.SizeX, sy = Grid.SizeY, sz = Grid.SizeZ;
        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
        for (int z = 0; z < sz; z++)
            Grid.Set(x, y, z, Random.Shared.NextDouble() < density);
    }

    public void Clear()
    {
        int sx = Grid.SizeX, sy = Grid.SizeY, sz = Grid.SizeZ;
        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
        for (int z = 0; z < sz; z++)
            Grid.Set(x, y, z, false);
    }

    /// <summary>
    /// Stamps a shape onto the grid at the given origin (wraps toroidally).
    /// Cells already alive are preserved; the shape only sets cells to alive.
    /// </summary>
    public void PlaceShape(Shape3D shape, int originX, int originY, int originZ)
    {
        int sx = Grid.SizeX, sy = Grid.SizeY, sz = Grid.SizeZ;
        foreach (var (dx, dy, dz) in shape.Cells)
        {
            int x = ((originX + dx) % sx + sx) % sx;
            int y = ((originY + dy) % sy + sy) % sy;
            int z = ((originZ + dz) % sz + sz) % sz;
            Grid.Set(x, y, z, true);
        }
    }
}
