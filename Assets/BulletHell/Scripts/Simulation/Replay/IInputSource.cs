using BulletHell.Simulation.Core;

public interface IInputSource
{
    bool TryGetInput(int tick, out InputFrame inputFrame);
}