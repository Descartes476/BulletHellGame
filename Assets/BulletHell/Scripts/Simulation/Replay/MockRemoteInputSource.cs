
using BulletHell.Simulation.Core;

public sealed class MockRemoteInputSource : IInputSource
{
    public bool TryGetInput(int tick, out InputFrame inputFrame)
    {
        inputFrame = CreateNeutralInput(tick);
        return true;
    }

    public int GetRecordMaxTick()
    {
        return 0;
    }

    private static InputFrame CreateNeutralInput(int tick)
    {
        return new InputFrame(tick, 0, 0, 0, 1, false);
    }
}
