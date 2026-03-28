using BulletHell.Simulation.Core;
using System.Collections.Generic;

public sealed class ReplayRecorder
{
    public ReplayData CurrentReplay { get; private set; }

    public void BeginRecording(in SimulationConfig config, int seed)
    {
        CurrentReplay = new ReplayData
        {
            Version = 1,
            TickRate = config.TickRate,
            Seed = seed,
            ConfigSnapshot = new ReplayConfigSnapshot
            {
                TickRate = config.TickRate,
                PlayerMoveSpeedRaw = config.PlayerMoveSpeed.RawValue,
                PlayerBulletSpeedRaw = config.PlayerBulletSpeed.RawValue,
                EnemyBulletSpeedRaw = config.EnemyBulletSpeed.RawValue,
                PlayerMaxHpRaw = config.PlayerMaxHp.RawValue,
                PlayerRespawnTicks = config.PlayerRespawnTicks,
                PlayerInvincibleTicks = config.PlayerInvincibleTicks
            },
            Frames = new List<ReplayFrame>()
        };
    }

    public void RecordFrame(int tick, in InputFrame inputFrame, ulong worldHash)
    {
        if(CurrentReplay == null)
        {
            return;
        }

        CurrentReplay.Frames.Add(new ReplayFrame
        {
            Tick = tick,
            Input = inputFrame,
            WorldHash = worldHash
        });
    }

    public ReplayData EndRecording()
    {
        ReplayData replayData = CurrentReplay;
        CurrentReplay = null;
        return replayData;
    }
}