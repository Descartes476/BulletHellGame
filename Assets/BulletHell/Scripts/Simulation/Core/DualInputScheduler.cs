
using BulletHell.Simulation.Core;

public class DualInputScheduler
{
    private readonly DualInputBuffer _buffer;
    private readonly IInputSource _p1InputSource;
    private readonly IInputSource _p2InputSource;
    private readonly int _p1InputDelayTicks;
    private readonly int _replayPrefetchTicks;
    
    public DualInputScheduler(
        DualInputBuffer buffer,
        IInputSource p1InputSource,
        IInputSource p2InputSource,
        int p1InputDelayTicks,
        int replayPrefetchTicks
        )
    {
        _buffer = buffer ?? throw new System.ArgumentNullException(nameof(buffer));
        _p1InputSource = p1InputSource;
        _p2InputSource = p2InputSource;
        _p1InputDelayTicks = p1InputDelayTicks;
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
        for(int tick = 0; tick < _p1InputDelayTicks; tick++)
        {
            if(!_buffer.HasP1(tick))
            {
                _buffer.StoreP1(tick, CreateNeutralInput(tick));
            }
        }
    }

    public bool IsReady(int tick, SimulationRunMode runMode)
    {
        return GetReadyState(tick, runMode) == InputReadyState.Ready;
    }

    public InputReadyState GetReadyState(int tick, SimulationRunMode runMode)
    {
        bool hasP1 = _buffer.HasP1(tick);
        if(runMode == SimulationRunMode.Replay)
        {
            return hasP1 ? InputReadyState.Ready : InputReadyState.MissingLocal;
        }

        bool hasP2 = _buffer.HasP2(tick);
 
        if(hasP1 && hasP2)
        {
            return InputReadyState.Ready;
        }
    
        if(!hasP1 && !hasP2)
        {
            return InputReadyState.MissingBoth;
        }
    
        if(!hasP1)
        {
            return InputReadyState.MissingLocal;
        }
    
        return InputReadyState.MissingRemote;
    }

    public bool TryConsume(int tick, SimulationRunMode runMode, out InputFrame p1Input, out InputFrame p2Input)
    {
        p1Input = default;
        p2Input = default;
        if(runMode == SimulationRunMode.Replay)
        {
            if(!_buffer.TryConsumeP1(tick, out p1Input))
            {
                return false;
            }
            p2Input = CreateNeutralInput(tick);
            return true;
        }

        if (!_buffer.TryConsumeP1(tick, out p1Input))
        {
            return false;
        }
 
        if (!_buffer.TryConsumeP2(tick, out p2Input))
        {
            return false;
        }

        return true;
    }

    private void FillLive(int currentTick)
    {
        FillP1Live(currentTick);
        FillP2Live(currentTick);
    }
    
    private void FillP1Live(int currentTick)
    {
        if(_p1InputSource == null)
        {
            return;
        }
        int targetTick = currentTick + _p1InputDelayTicks;
        if(_buffer.HasP1(targetTick))
        {
            return;
        }
        if(_p1InputSource.TryGetInput(targetTick, out InputFrame input))
        {
            _buffer.StoreP1(targetTick, input);
        }
    }

    private void FillP2Live(int currentTick)
    {
        if (_p2InputSource == null)
        {
            return;
        }
 
        if (_buffer.HasP2(currentTick))
        {
            return;
        }
 
        if (_p2InputSource.TryGetInput(currentTick, out InputFrame input))
        {
            _buffer.StoreP2(currentTick, input);
        }
    }

    private void FillReplay(int currentTick)
    {
        FillP1Replay(currentTick);
        FillP2Replay(currentTick);
    }

    private void FillP1Replay(int currentTick)
    {
        if (_p1InputSource == null)
        {
            return;
        }
 
        int endTickExclusive = currentTick + _replayPrefetchTicks;
        for (int tick = currentTick; tick < endTickExclusive; tick++)
        {
            if (_buffer.HasP1(tick))
            {
                continue;
            }
 
            if (_p1InputSource.TryGetInput(tick, out InputFrame input))
            {
                _buffer.StoreP1(tick, input);
            }
        }
    }

    private void FillP2Replay(int currentTick)
    {
        if (_p2InputSource == null)
        {
            return;
        }
 
        if (_buffer.HasP2(currentTick))
        {
            return;
        }
 
        if (_p2InputSource.TryGetInput(currentTick, out InputFrame input))
        {
            _buffer.StoreP2(currentTick, input);
        }
    }

    private static InputFrame CreateNeutralInput(int tick)
    {
        return new InputFrame(tick, 0, 0, 0, 1, false);
    }

    public int GetRecordMaxTick()
    {
        return _p1InputSource != null ? _p1InputSource.GetRecordMaxTick() : 0;
    }
}
