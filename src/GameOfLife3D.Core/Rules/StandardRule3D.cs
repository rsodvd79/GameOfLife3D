#nullable enable

using System;

namespace GameOfLife3D.Core.Rules;

public class StandardRule3D : IRule3D
{
    // "445" rule: survive on 5,6,7 neighbors; born on 6
    public int[] SurvivalCounts { get; set; } = { 5, 6, 7 };
    public int[] BirthCounts { get; set; } = { 6 };

    public bool NextState(bool currentState, int neighborCount)
    {
        if (currentState)
            return Array.IndexOf(SurvivalCounts, neighborCount) >= 0;
        else
            return Array.IndexOf(BirthCounts, neighborCount) >= 0;
    }
}
