using BulletHell.Simulation.Core;

public sealed class DualInputBuffer
{
    private readonly InputBuffer _localBuffer = new InputBuffer();
    private readonly InputBuffer _remoteBuffer = new InputBuffer();

    public void StoreLocal(int tick, in InputFrame input)
    {
        _localBuffer.Store(tick, input);
    }

    public void StoreRemote(int tick, in InputFrame input)
    {
        _remoteBuffer.Store(tick, input);
    }

    public bool HasLocal(int tick)
    {
        return _localBuffer.HasInput(tick);
    }

    public bool HasRemote(int tick)
    {
        return _remoteBuffer.HasInput(tick);
    }

    public bool TryConsumeLocal(int tick, out InputFrame input)
    {
        return _localBuffer.TryConsume(tick, out input);
    }

    public bool TryConsumeRemote(int tick, out InputFrame input)
    {
        return _remoteBuffer.TryConsume(tick, out input);
    }

    public bool IsReady(int tick)
    {
        return HasLocal(tick) && HasRemote(tick);
    }

    public void Clear()
    {
        _localBuffer.Clear();
        _remoteBuffer.Clear();
    }

}
