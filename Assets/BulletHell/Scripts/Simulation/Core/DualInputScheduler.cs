
using BulletHell.Simulation.Core;

public class DualInputScheduler
{
    private readonly DualInputBuffer _buffer;
    private readonly IInputSource _localInputSource;
    private readonly IInputSource _remoteInputSource;
    private readonly int _localInputDelayTicks;
    private readonly int _replayPrefetchTicks;
    
    public DualInputScheduler(
        DualInputBuffer buffer,
        IInputSource localInputSource,
        IInputSource remoteInputSource,
        int localInputDelayTicks,
        int replayPrefetchTicks
        )
    {
        _buffer = buffer ?? throw new System.ArgumentNullException(nameof(buffer));
        _localInputSource = localInputSource;
        _remoteInputSource = remoteInputSource;
        _localInputDelayTicks = localInputDelayTicks;
        _replayPrefetchTicks = replayPrefetchTicks;
    }

    public void Fill(int currentTick, SimulationRunMode runMode)
    {
        if(runMode == SimulationRunMode.Live)
        {
            FillLive(currentTick);
            return;
        }
        FillReplay(currentTick);
    }

    public void WarmupLive()
    {
        for(int tick = 0; tick < _localInputDelayTicks; tick++)
        {
            if(!_buffer.HasLocal(tick))
            {
                _buffer.StoreLocal(tick, CreateNeutralInput(tick));
            }
        }
    }

    public bool IsReady(int tick, SimulationRunMode runMode)
    {
        if(runMode == SimulationRunMode.Replay)
        {
            return _buffer.HasLocal(tick);
        }
        return _buffer.HasLocal(tick) && _buffer.HasRemote(tick);
    }

    public bool TryConsume(int tick, SimulationRunMode runMode, out InputFrame localInput, out InputFrame remoteInput)
    {
        localInput = default;
        remoteInput = default;
        if(runMode == SimulationRunMode.Replay)
        {
            if(!_buffer.TryConsumeLocal(tick, out localInput))
            {
                return false;
            }
            remoteInput = CreateNeutralInput(tick);
            return true;
        }

        if (!_buffer.TryConsumeLocal(tick, out localInput))
        {
            return false;
        }
 
        if (!_buffer.TryConsumeRemote(tick, out remoteInput))
        {
            return false;
        }

        return true;
    }

    private void FillLive(int currentTick)
    {
        FillLocalLive(currentTick);
        FillRemoteLive(currentTick);
    }
    
    private void FillLocalLive(int currentTick)
    {
        if(_localInputSource == null)
        {
            return;
        }
        int targetTick = currentTick + _localInputDelayTicks;
        if(_buffer.HasLocal(targetTick))
        {
            return;
        }
        if(_localInputSource.TryGetInput(targetTick, out InputFrame input))
        {
            _buffer.StoreLocal(targetTick, input);
        }
    }

    private void FillRemoteLive(int currentTick)
    {
        if (_remoteInputSource == null)
        {
            return;
        }
 
        if (_buffer.HasRemote(currentTick))
        {
            return;
        }
 
        if (_remoteInputSource.TryGetInput(currentTick, out InputFrame input))
        {
            _buffer.StoreRemote(currentTick, input);
        }
    }

    private void FillReplay(int currentTick)
    {
        FillLocalReplay(currentTick);
        FillRemoteReplay(currentTick);
    }

    private void FillLocalReplay(int currentTick)
    {
        if (_localInputSource == null)
        {
            return;
        }
 
        int endTickExclusive = currentTick + _replayPrefetchTicks;
        for (int tick = currentTick; tick < endTickExclusive; tick++)
        {
            if (_buffer.HasLocal(tick))
            {
                continue;
            }
 
            if (_localInputSource.TryGetInput(tick, out InputFrame input))
            {
                _buffer.StoreLocal(tick, input);
            }
        }
    }

    private void FillRemoteReplay(int currentTick)
    {
        if (_remoteInputSource == null)
        {
            return;
        }
 
        if (_buffer.HasRemote(currentTick))
        {
            return;
        }
 
        if (_remoteInputSource.TryGetInput(currentTick, out InputFrame input))
        {
            _buffer.StoreRemote(currentTick, input);
        }
    }

    private static InputFrame CreateNeutralInput(int tick)
    {
        return new InputFrame(tick, 0, 0, 0, 1, false);
    }

    public int GetRecordMaxTick()
    {
        return _localInputSource != null ? _localInputSource.GetRecordMaxTick() : 0;
    }
}
