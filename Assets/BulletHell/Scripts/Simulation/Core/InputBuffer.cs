using System.Collections.Generic;
using BulletHell.Simulation.Core;

public class InputBuffer
{
    private readonly Dictionary<int, InputFrame> _buffers = new Dictionary<int, InputFrame>();

    public void Store(int tick, in InputFrame input)
    {
        _buffers[tick] = input;
    }

    public bool TryGetInput(int tick, out InputFrame input)
    {
        return _buffers.TryGetValue(tick, out input);
    }

    public bool HasInput(int tick)
    {
        return _buffers.ContainsKey(tick);
    }

    public bool TryConsume(int tick, out InputFrame input)
    {
        bool result = TryGetInput(tick, out input);
        if(result)
        {
            _buffers.Remove(tick);
        }

        return result;
    }

    public void Remove(int tick)
    {
        _buffers.Remove(tick);
    }

    public void Clear()
    {
        _buffers.Clear();
    }

}
