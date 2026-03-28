using BulletHell.Simulation.Core;

public struct StableHashBuilder
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    private ulong _value;

    public StableHashBuilder(ulong seed)
    {
        _value = seed == 0 ? OffsetBasis : seed;
    }

    public void Add(int value)
    {
        Add((long)value);
    }

    public void Add(long value)
    {
        unchecked
        {
            _value ^= (ulong)value;
            _value *= Prime;
        }
    }

    public void Add(bool value)
    {
        Add(value ? 1 : 0);
    }

    public void Add(Fix64 value)
    {
        Add(value.RawValue);
    }

    public void Add(FixVector2 value)
    {
        Add(value.x);
        Add(value.y);
    }

    public void Add(FixVector3 value)
    {
        Add(value.x);
        Add(value.y);
        Add(value.z);
    }

    public ulong ToHash()
    {
        return _value == 0 ? OffsetBasis : _value;
    }
}