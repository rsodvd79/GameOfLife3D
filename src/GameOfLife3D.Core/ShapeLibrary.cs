#nullable enable

using System;
using System.Linq;

namespace GameOfLife3D.Core;

/// <summary>A named pattern of relative cell offsets to stamp onto the grid.</summary>
public record Shape3D(string Name, (int dx, int dy, int dz)[] Cells)
{
    public override string ToString() => Name;
}

/// <summary>
/// Built-in 3D patterns, verified against the default S5,6,7 / B6 ("445") rule.
///
/// Categories:
///   🔒 Still Life  — mathematically stable: never changes.
///   🌱 Seed        — evolves into a stable structure within 1-2 steps.
///   📐 Geometric   — interesting starting shape; evolution depends on the active rule.
/// </summary>
public static class ShapeLibrary
{
    // ── Still Lifes (verified stable under S5,6,7 / B6) ──────────────────────
    //
    // Block 2×2×2: each cell has 7 live neighbours → survives.
    // Adjacent empty cells have ≤4 neighbours → never born.
    private static readonly Shape3D BlockStillLife = new("🔒 Block 2×2×2", [
        (0,0,0),(1,0,0),(0,1,0),(1,1,0),
        (0,0,1),(1,0,1),(0,1,1),(1,1,1),
    ]);

    // Cross: center has 6 neighbours; each arm has 5 (centre + 4 orthogonal arms) → all survive.
    private static readonly Shape3D CrossStillLife = new("🔒 Cross", [
        ( 0, 0, 0),
        ( 1, 0, 0),(-1, 0, 0),
        ( 0, 1, 0),( 0,-1, 0),
        ( 0, 0, 1),( 0, 0,-1),
    ]);

    // Twin Blocks: two isolated 2×2×2 blocks placed 4 units apart.
    // Each block is independently stable; the gap prevents interaction.
    private static readonly Shape3D TwinBlocks = new("🔒 Twin Blocks", [
        (0,0,0),(1,0,0),(0,1,0),(1,1,0),(0,0,1),(1,0,1),(0,1,1),(1,1,1),
        (5,0,0),(6,0,0),(5,1,0),(6,1,0),(5,0,1),(6,0,1),(5,1,1),(6,1,1),
    ]);

    // Rhombicuboctahedron (32 cells): all integer lattice points (dx,dy,dz)
    // with L1-norm = 3 and L∞-norm ≤ 2.  Emerges spontaneously from Cube/Shell seeds
    // and is provably stable.
    private static readonly Shape3D Rhombicuboctahedron = new("🔒 Rhombicuboctahedron (32)", (
        from dx in Enumerable.Range(-2, 5)
        from dy in Enumerable.Range(-2, 5)
        from dz in Enumerable.Range(-2, 5)
        where Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) == 3
           && Math.Max(Math.Abs(dx), Math.Max(Math.Abs(dy), Math.Abs(dz))) <= 2
        select (dx, dy, dz)).ToArray());

    // ── Seeds (converge to a still life in 1–2 steps) ────────────────────────

    // Shell 3×3×3 (26 cells) → Rhombicuboctahedron in exactly 1 step.
    private static readonly Shape3D ShellSeed = new("🌱 Shell 3×3×3", (
        from dx in new[] { -1, 0, 1 }
        from dy in new[] { -1, 0, 1 }
        from dz in new[] { -1, 0, 1 }
        where !(dx == 0 && dy == 0 && dz == 0)
        select (dx, dy, dz)).ToArray());

    // Solid Cube 3×3×3 (27 cells) → Rhombicuboctahedron in exactly 1 step.
    private static readonly Shape3D CubeSeed = new("🌱 Cube 3×3×3", (
        from dx in new[] { -1, 0, 1 }
        from dy in new[] { -1, 0, 1 }
        from dz in new[] { -1, 0, 1 }
        select (dx, dy, dz)).ToArray());

    // 3×3×2 Slab (18 cells) → 24-cell stable shell in 1 step.
    private static readonly Shape3D SlabSeed = new("🌱 Slab 3×3×2", (
        from dx in new[] { -1, 0, 1 }
        from dz in new[] { -1, 0, 1 }
        from dy in new[] { 0, 1 }
        select (dx, dy, dz)).ToArray());

    // ── Geometric shapes (free-form seeds) ───────────────────────────────────

    private static readonly Shape3D SingleCell = new("📐 Single Cell", [(0, 0, 0)]);

    private static readonly Shape3D Plane3x3 = new("📐 Plane 3×3", [
        (-1,-1, 0),(0,-1, 0),(1,-1, 0),
        (-1, 0, 0),(0, 0, 0),(1, 0, 0),
        (-1, 1, 0),(0, 1, 0),(1, 1, 0),
    ]);

    private static readonly Shape3D Ring = new("📐 Ring", [
        (-1,-1, 0),(0,-1, 0),(1,-1, 0),
        (-1, 0, 0),          (1, 0, 0),
        (-1, 1, 0),(0, 1, 0),(1, 1, 0),
    ]);

    private static readonly Shape3D Pillar = new("📐 Pillar 1×5×1", [
        (0,-2, 0),(0,-1, 0),(0, 0, 0),(0, 1, 0),(0, 2, 0),
    ]);

    // 3D star: centre + 2-step arms along each axis (13 cells).
    private static readonly Shape3D Star = new("📐 Star", [
        (0, 0, 0),
        ( 1, 0, 0),( 2, 0, 0),(-1, 0, 0),(-2, 0, 0),
        ( 0, 1, 0),( 0, 2, 0),( 0,-1, 0),( 0,-2, 0),
        ( 0, 0, 1),( 0, 0, 2),( 0, 0,-1),( 0, 0,-2),
    ]);

    // Diamond octahedron: all cells with L1-norm ≤ 2 (19 cells).
    private static readonly Shape3D Octahedron = new("📐 Octahedron", (
        from dx in Enumerable.Range(-2, 5)
        from dy in Enumerable.Range(-2, 5)
        from dz in Enumerable.Range(-2, 5)
        where Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) <= 2
        select (dx, dy, dz)).ToArray());

    // ── Public catalogue ──────────────────────────────────────────────────────

    public static readonly Shape3D[] All =
    [
        // Still lifes first (most useful for stable seeding)
        BlockStillLife,
        CrossStillLife,
        TwinBlocks,
        Rhombicuboctahedron,
        // Seeds
        ShellSeed,
        CubeSeed,
        SlabSeed,
        // Free-form geometric
        SingleCell,
        Plane3x3,
        Ring,
        Pillar,
        Star,
        Octahedron,
    ];
}
