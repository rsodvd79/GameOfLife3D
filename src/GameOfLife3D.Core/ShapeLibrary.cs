#nullable enable

using System;
using System.Linq;

namespace GameOfLife3D.Core;

/// <summary>A named pattern of relative cell offsets to stamp onto the grid.</summary>
public record Shape3D(string Name, (int dx, int dy, int dz)[] Cells)
{
    /// <summary>
    /// Optional rule under which the shape behaves as documented. When set,
    /// <c>MainViewModel.PlaceShape</c> applies it before stamping the shape so
    /// the intended behaviour (e.g. a glider actually translating) is visible
    /// without the user having to edit the rule fields by hand.
    /// </summary>
    public (int[] Survive, int[] Birth)? RecommendedRule { get; init; }

    public override string ToString() => Name;
}

/// <summary>
/// Built-in 3D patterns.
///
/// Categories:
///   🔒 Still Life  — mathematically stable under the default S5,6,7 / B6 ("445") rule.
///   🌱 Seed        — evolves into a stable structure within 1-2 steps (under "445").
///   🔁 Oscillator  — cycles back to its original configuration after N steps (Blinker: period 2 under "445").
///   🚀 Spaceship   — translates across the grid; requires its <see cref="Shape3D.RecommendedRule"/> (e.g. Bays' glider needs S4,7 / B5).
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

    // 3×2×3 Slab (18 cells) → 24-cell stable shell in 1 step.
    private static readonly Shape3D SlabSeed = new("🌱 Slab 3×2×3", (
        from dx in new[] { -1, 0, 1 }
        from dz in new[] { -1, 0, 1 }
        from dy in new[] { 0, 1 }
        select (dx, dy, dz)).ToArray());

    // ── Oscillators ──────────────────────────────────────────────────────────

    // Blinker 2×3×1: a flat 2-wide × 3-tall rectangle (period-2 oscillator).
    // Under rules that support it (e.g. S4,5 / B5) the slab alternates between
    // its original orientation and a rotated 3×2×1 configuration each step.
    private static readonly Shape3D Blinker2x3x1 = new("🔁 Blinker 2×3×1", [
        (0,-1, 0),(1,-1, 0),
        (0, 0, 0),(1, 0, 0),
        (0, 1, 0),(1, 1, 0),
    ]);

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

    // ── Spaceships (move across the grid; need a specific rule) ───────────────

    // Bays' period-8 glider (Carter Bays, "A Note About the Discovery of Many New
    // Rules for the Game of Three-Dimensional Life", Complex Systems 16(4), 2006).
    // Under rule S4,7 / B5 it translates 4 cells per period (speed 1/2 c along an
    // axis). Verified in this engine: normalized pattern repeats every 8 steps with
    // a steady translation. Under the default "445" rule it does NOT move.
    private static readonly Shape3D BaysGlider = new("🚀 Glider (Bays 4,7/5)", [
        (0,3,5),(0,4,5),(1,3,6),(1,4,6),(2,2,4),(2,2,5),(2,2,6),(2,3,2),(2,4,2),(2,5,4),
        (2,5,5),(2,5,6),(3,1,6),(3,2,2),(3,2,6),(3,3,2),(3,4,2),(3,5,2),(3,5,6),(3,6,6),
        (4,1,6),(4,2,2),(4,2,6),(4,3,2),(4,4,2),(4,5,2),(4,5,6),(4,6,6),(5,2,4),(5,2,5),
        (5,2,6),(5,3,2),(5,4,2),(5,5,4),(5,5,5),(5,5,6),(6,3,6),(6,4,6),(7,3,5),(7,4,5),
    ])
    {
        RecommendedRule = (new[] { 4, 7 }, new[] { 5 }),
    };

    // Bays' period-4 glider (Carter Bays, 2006). Under rule S5,7 / B6 it translates
    // (0,-1,+1) per period; the paper notes it is also supported by the default
    // S5,6,7 / B6 ("445") rule — verified in this engine: period 4, pop 10, steady drift.
    private static readonly Shape3D BaysGlider57 = new("🚀 Glider (Bays 5,7/6)", [
        (0,0,0),(0,0,1),(0,0,2),(0,1,2),(0,2,1),
        (1,0,0),(1,0,1),(1,0,2),(1,1,2),(1,2,1),
    ])
    {
        RecommendedRule = (new[] { 5, 6, 7 }, new[] { 6 }),
    };

    // Bays' period-8 glider under rule S8 / B5 (also supported by S6,7,8 / B5).
    // Verified in this engine: period 8, pop 28, translates (4,-4,0) per period.
    private static readonly Shape3D BaysGlider85 = new("🚀 Glider (Bays 8/5)", [
        (0,1,3),(0,1,4),(1,0,3),(1,0,4),(1,1,3),(1,1,4),(1,3,3),(1,3,4),
        (2,0,3),(2,0,4),(2,1,2),(2,1,3),(2,1,4),(2,1,5),(2,4,3),(2,4,4),
        (4,3,2),(4,3,3),(4,3,4),(4,3,5),(4,4,3),(4,4,4),(4,5,3),(4,5,4),
        (5,3,3),(5,3,4),(5,4,3),(5,4,4),
    ])
    {
        RecommendedRule = (new[] { 8 }, new[] { 5 }),
    };

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
        // Oscillators
        Blinker2x3x1,
        // Free-form geometric
        SingleCell,
        Plane3x3,
        Ring,
        Pillar,
        Star,
        Octahedron,
        // Spaceships
        BaysGlider,
        BaysGlider57,
        BaysGlider85,
    ];
}
