using System.Collections.Generic;
using BulletHell.Simulation.Core;

public sealed class ReplayInputSource : IInputSource
{
    private ReplayData _replayData;

    public ReplayInputSource(ReplayData replayData)
    {
        if(replayData == null)
        {
            throw new System.ArgumentNullException(nameof(replayData));
        }

        _replayData = replayData;
    }

    public bool TryGetInput(int tick, out InputFrame inputFrame)
    {
        List<ReplayFrame> frames = _replayData.Frames;

        if(frames == null || frames.Count == 0)
        {
            inputFrame = default;
            return false;
        }

        for(int i=0; i<frames.Count; i++)
        {
            ReplayFrame curFrame = frames[i];
            if(curFrame.Tick == tick)
            {
                inputFrame = curFrame.Input;
                return true;
            }
        }

        inputFrame = default;
        return false;
    }
}