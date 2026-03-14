#nullable enable

namespace GameOfLife3D.Core.Rules;

public interface IRule3D
{
    bool NextState(bool currentState, int neighborCount);
}
