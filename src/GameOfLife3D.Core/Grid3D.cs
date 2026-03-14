#nullable enable

using System.Collections.Generic;

namespace GameOfLife3D.Core;

public class Grid3D
{
    private bool[,,] _front;
    private bool[,,] _back;
    private readonly object _lock = new();

    public int SizeX { get; }
    public int SizeY { get; }
    public int SizeZ { get; }

    public Grid3D(int sizeX, int sizeY, int sizeZ)
    {
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
        _front = new bool[sizeX, sizeY, sizeZ];
        _back  = new bool[sizeX, sizeY, sizeZ];
    }

    public bool Get(int x, int y, int z)
    {
        lock (_lock) return _front[x, y, z];
    }

    public void Set(int x, int y, int z, bool value)
    {
        lock (_lock) _front[x, y, z] = value;
    }

    // Write to back buffer (called from Step, single writer, no lock needed for back)
    internal bool GetBack(int x, int y, int z) => _back[x, y, z];
    internal void SetBack(int x, int y, int z, bool value) => _back[x, y, z] = value;

    // Read from front buffer without lock (used inside Step loop – single reader, writer uses back)
    internal bool GetFront(int x, int y, int z) => _front[x, y, z];

    public void Swap()
    {
        lock (_lock)
        {
            (_front, _back) = (_back, _front);
        }
    }

    public int CountNeighbors(int x, int y, int z)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        for (int dz = -1; dz <= 1; dz++)
        {
            if (dx == 0 && dy == 0 && dz == 0) continue;
            int nx = (x + dx + SizeX) % SizeX;
            int ny = (y + dy + SizeY) % SizeY;
            int nz = (z + dz + SizeZ) % SizeZ;
            if (GetFront(nx, ny, nz)) count++;
        }
        return count;
    }

    public IEnumerable<(int x, int y, int z)> GetLiveCells()
    {
        bool[,,] snapshot;
        lock (_lock) snapshot = _front;
        for (int x = 0; x < SizeX; x++)
        for (int y = 0; y < SizeY; y++)
        for (int z = 0; z < SizeZ; z++)
            if (snapshot[x, y, z])
                yield return (x, y, z);
    }
}
