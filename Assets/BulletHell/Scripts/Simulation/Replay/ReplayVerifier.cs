using System.Collections.Generic;

public struct ReplayMismatchInfo
{
    public int Tick;
    public ulong ExpectedHash;
    public ulong ActualHash;
}

public sealed class ReplayVerifier
{
    public ReplayData ReplayData { get; private set; }
    public bool HasMismatch { get; private set; }
    public ReplayMismatchInfo FirstMismatch { get; private set; }

    public void Load(ReplayData replayData)
    {
        if(replayData == null)
        {
            throw new System.ArgumentNullException(nameof(replayData));
        }

        ReplayData = replayData;
        Reset();
    }

    public bool Verify(int tick, ulong actualHash, out ReplayMismatchInfo mismatchInfo)
    {
        if(ReplayData == null)
        {
            throw new System.ArgumentNullException(nameof(ReplayData));
        }
        if(!TryGetExpectedHash(tick, out ulong expectedHash))
        {
            throw new System.InvalidOperationException(nameof(expectedHash));
        }
        if(actualHash == expectedHash)
        {
            mismatchInfo = default;
            return true;
        }
        mismatchInfo = new ReplayMismatchInfo
        {
            Tick = tick,
            ExpectedHash = expectedHash,
            ActualHash = actualHash
        };

        if(!HasMismatch)
        {
            HasMismatch = true;
            FirstMismatch = mismatchInfo;
        }

        return false;
    }

    public bool TryGetExpectedHash(int tick, out ulong expectedHash)
    {
        if(ReplayData == null || ReplayData.Frames == null)
        {
            expectedHash = default;
            return false;
        }

        List<ReplayFrame> frames = ReplayData.Frames;
        for(int i=0; i<frames.Count; i++)
        {
            if(frames[i].Tick == tick)
            {
                expectedHash = frames[i].WorldHash;
                return true;
            }
        }
        expectedHash = default;
        return false;
    }

    public void Reset()
    {
        HasMismatch = false;
        FirstMismatch = default;
    }
}
