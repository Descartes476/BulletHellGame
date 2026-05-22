using BulletHell.Simulation.Core;

public sealed class DualInputBuffer
{
    private readonly InputBuffer _p1Buffer = new InputBuffer();
    private readonly InputBuffer _p2Buffer = new InputBuffer();

    public void StoreP1(int tick, in InputFrame input)
    {
        _p1Buffer.Store(tick, input);
    }

    public void StoreP2(int tick, in InputFrame input)
    {
        _p2Buffer.Store(tick, input);
    }

    public bool HasP1(int tick)
    {
        return _p1Buffer.HasInput(tick);
    }

    public bool HasP2(int tick)
    {
        return _p2Buffer.HasInput(tick);
    }

    public bool TryConsumeP1(int tick, out InputFrame input)
    {
        return _p1Buffer.TryConsume(tick, out input);
    }

    public bool TryConsumeP2(int tick, out InputFrame input)
    {
        return _p2Buffer.TryConsume(tick, out input);
    }

    public bool IsReady(int tick)
    {
        return HasP1(tick) && HasP2(tick);
    }

    public void Clear()
    {
        _p1Buffer.Clear();
        _p2Buffer.Clear();
    }

}
