#nullable enable

using System;
using GameOfLife3D.Core.Rules;

namespace GameOfLife3D.Core;

public class SimulationEngine
{
    public Grid3D Grid { get; }
    public IRule3D Rule { get; set; }

    public SimulationEngine(Grid3D grid, IRule3D rule)
    {
        Grid = grid;
        Rule = rule;
    }

    public void Step()
    {
        int sx = Grid.SizeX, sy = Grid.SizeY, sz = Grid.SizeZ;
        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
        for (int z = 0; z < sz; z++)
        {
            bool current = Grid.GetFront(x, y, z);
            int neighbors = Grid.CountNeighbors(x, y, z);
            Grid.SetBack(x, y, z, Rule.NextState(current, neighbors));
        }
        Grid.Swap();
    }

    public void Randomize(double density)
    {
        var rng = new Random();
        int sx = Grid.SizeX, sy = Grid.SizeY, sz = Grid.SizeZ;
        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
        for (int z = 0; z < sz; z++)
            Grid.Set(x, y, z, rng.NextDouble() < density);
    }

    public void Clear()
    {
        int sx = Grid.SizeX, sy = Grid.SizeY, sz = Grid.SizeZ;
        for (int x = 0; x < sx; x++)
        for (int y = 0; y < sy; y++)
        for (int z = 0; z < sz; z++)
            Grid.Set(x, y, z, false);
    }
}
