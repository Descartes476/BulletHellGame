using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class DeterministicRandom
{
    private const uint DefaultSeed = 2463534242u;
    private const uint FixUnit = 1u << Fix64.FRACTIONAL_PLACES;

    private uint _state;

    public DeterministicRandom(uint seed)
    {
        _state = seed == 0u ? DefaultSeed : seed;
    }

    public uint NextUInt()
    {
        uint x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _state = x;
        return _state;
    }

    public float Next01()
    {
        return (NextUInt() & 0x00FFFFFFu) / 16777216f;
    }

    public Fix64 NextFix01()
    {
        return Fix64.FromRaw(NextUInt() % FixUnit);
    }

    public int RangeInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new System.ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be greater than minInclusive.");
        }

        uint span = (uint)(maxExclusive - minInclusive);
        return minInclusive + (int)(NextUInt() % span);
    }

    public float RangeFloat(float minInclusive, float maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new System.ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be greater than minInclusive.");
        }

        return minInclusive + (maxExclusive - minInclusive) * Next01();
    }

    public Fix64 RangeFix(Fix64 minInclusive, Fix64 maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new System.ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be greater than minInclusive.");
        }

        return minInclusive + (maxExclusive - minInclusive) * NextFix01();
    }

}
