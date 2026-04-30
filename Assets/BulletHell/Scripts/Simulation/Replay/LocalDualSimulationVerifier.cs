using BulletHell.Simulation.Core;

public struct DualSimVerificationResult
{
    public bool IsMatch;
    public int MismatchTick;
    public ulong HashA;
    public ulong HashB;
    public ulong ExpectedReplayHash;
    public string ErrorMessage; // 可选，用于调试信息
}

// 本地双模拟验证器
public class LocalDualSimulationVerifier
{
    public DualSimVerificationResult Verify(ReplayData replayData, WorldSnapshot initialWorld, SimulationConfig config)
    {
        var runnerA = new DeterministicSimulationRunner(initialWorld, config, 0);
        var runnerB = new DeterministicSimulationRunner(initialWorld, config, 0);
        
        // 检查初始 hash
        if (runnerA.CurrentHash != runnerB.CurrentHash)
        {
            return new DualSimVerificationResult
            {
                IsMatch = false,
                MismatchTick = 0,
                HashA = runnerA.CurrentHash,
                HashB = runnerB.CurrentHash,
                ErrorMessage = "初始世界Hash不一致"
            };
        }
        
        for(int i = 0; i < replayData.Frames.Count; i++)
        {
            var frame = replayData.Frames[i];
            runnerA.Step(frame.Input);
            runnerB.Step(frame.Input);
            if(runnerA.CurrentHash != runnerB.CurrentHash)
            {
                return new DualSimVerificationResult
                {
                    IsMatch = false,
                    MismatchTick = frame.Tick,
                    HashA = runnerA.CurrentHash,
                    HashB = runnerB.CurrentHash,
                    ExpectedReplayHash = frame.WorldHash,
                    ErrorMessage = $"Tick {frame.Tick} 处 hash 不一致"
                };
            }

        }
        return new DualSimVerificationResult
        {
            IsMatch = true,
            MismatchTick = -1,
            HashA = runnerA.CurrentHash,
            HashB = runnerB.CurrentHash
        };
    }
}
