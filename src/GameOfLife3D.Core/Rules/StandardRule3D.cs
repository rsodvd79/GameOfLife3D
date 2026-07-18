#nullable enable

using System;

namespace GameOfLife3D.Core.Rules;

public class StandardRule3D : IRule3D
{
    // "445" rule: survive on 5,6,7 neighbors; born on 6
    private int[] _survivalCounts = { 5, 6, 7 };
    private int[] _birthCounts = { 6 };

    public int[] SurvivalCounts
    {
        get => _survivalCounts;
        set => _survivalCounts = value ?? throw new ArgumentNullException(nameof(value));
    }

    public int[] BirthCounts
    {
        get => _birthCounts;
        set => _birthCounts = value ?? throw new ArgumentNullException(nameof(value));
    }

    public bool NextState(bool currentState, int neighborCount)
    {
        if (currentState)
            return Array.IndexOf(_survivalCounts, neighborCount) >= 0;
        else
            return Array.IndexOf(_birthCounts, neighborCount) >= 0;
    }
}
