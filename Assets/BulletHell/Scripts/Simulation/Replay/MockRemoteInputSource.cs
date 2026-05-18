
using BulletHell.Simulation.Core;

public sealed class MockRemoteInputSource : IInputSource
{

    public bool TryGetInput(int tick, out InputFrame inputFrame)
    {
        sbyte moveX = 0;
        sbyte moveY = 0;
    
        if ((tick / 120) % 2 == 0)
        {
            moveX = 1;
        }
        else
        {
            moveX = -1;
        }
    
        bool fire = tick % 20 == 0;
    
        inputFrame = new InputFrame(
            tick,
            moveX,
            moveY,
            0,
            1000,
            fire
        );
    
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
