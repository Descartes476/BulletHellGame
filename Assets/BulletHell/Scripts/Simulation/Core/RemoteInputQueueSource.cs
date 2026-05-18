using System.Collections.Generic;
using BulletHell.Simulation.Core;

public sealed class RemoteInputQueueSource : IInputSource
{
    private readonly Dictionary<int, InputFrame> _inputs = new Dictionary<int, InputFrame>();
    private int _recordMaxTick;

    public void PushInput(InputFrame inputFrame)
    {
        _inputs[inputFrame.Tick] = inputFrame;

        if(inputFrame.Tick > _recordMaxTick)
        {
            _recordMaxTick = inputFrame.Tick;
        }
    }

    public bool TryGetInput(int tick, out InputFrame inputFrame)
    {
        return _inputs.TryGetValue(tick, out inputFrame);
    }

    public int GetRecordMaxTick()
    {
        return _recordMaxTick;
    }

    public void Clear()
    {
        _inputs.Clear();
        _recordMaxTick = 0;
    }
}
