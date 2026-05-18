using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletHell.Simulation.Core;

public class DeterminismValidationRunner : MonoBehaviour
{
    private const int ValidationTicks = 1000;

    private struct ValidationResult
    {
        public string TestName;
        public bool Passed;
        public int Ticks;
        public uint Seed;
        public ulong FinalHash;
        public int MismatchTick;
    }

    
    private SimulationConfig _config;

    [ContextMenu("Run Determinism Validation")]
    public void RunValidation()
    {
        _config = CreateTestConfig();
        EmptyInputTicksShouldMatch();
        MoveAndFireInputTicksShouldMatch();
        DifferentSeedShouldMisMatch();
        ReplayRecordedRunShouldMatch();
    }

    private void EmptyInputTicksShouldMatch()
    {
        const uint seed = 0;
        var initialWorld = CreateTestInitialWorld();
        var runnerA = new DeterministicSimulationRunner(initialWorld, _config, seed);
        var runnerB = new DeterministicSimulationRunner(initialWorld, _config, seed);

        for(int tick = 0; tick < ValidationTicks; tick++)
        {
            InputFrame localInput = CreateNeutralInput(tick);
            InputFrame remoteInput = CreateNeutralInput(tick);
            FrameInputBundle bundle = new FrameInputBundle(tick, localInput, remoteInput);
            runnerA.Step(bundle);
            runnerB.Step(bundle);
            if(runnerA.CurrentHash != runnerB.CurrentHash)
            {
                LogResult(new ValidationResult
                {
                    TestName = "空输入测试",
                    Passed = false,
                    Ticks = tick + 1,
                    Seed = seed,
                    FinalHash = runnerA.CurrentHash,
                    MismatchTick = tick
                });
                return;
            }
        }
        LogResult(new ValidationResult
        {
            TestName = "空输入测试",
            Passed = true,
            Ticks = ValidationTicks,
            Seed = seed,
            FinalHash = runnerA.CurrentHash,
            MismatchTick = -1
        });
    }

    private void MoveAndFireInputTicksShouldMatch()
    {
        const uint seed = 0;
        var initialWorld = CreateTestInitialWorld();
        var runnerA = new DeterministicSimulationRunner(initialWorld, _config, seed);
        var runnerB = new DeterministicSimulationRunner(initialWorld, _config, seed);

        for(int tick = 0; tick < ValidationTicks; tick++)
        {
            InputFrame localInput = CreateMoveAndFireInput(tick);
            InputFrame remoteInput = CreateMoveAndFireInput(tick);
            FrameInputBundle bundle = new FrameInputBundle(tick, localInput, remoteInput);
            runnerA.Step(bundle);
            runnerB.Step(bundle);
            if(runnerA.CurrentHash != runnerB.CurrentHash)
            {
                LogResult(new ValidationResult
                {
                    TestName = "移动和射击输入测试",
                    Passed = false,
                    Ticks = tick + 1,
                    Seed = seed,
                    FinalHash = runnerA.CurrentHash,
                    MismatchTick = tick
                });
                return;
            }
        }
        LogResult(new ValidationResult
        {
            TestName = "移动和射击输入测试",
            Passed = true,
            Ticks = ValidationTicks,
            Seed = seed,
            FinalHash = runnerA.CurrentHash,
            MismatchTick = -1
        });
    }

    private void DifferentSeedShouldMisMatch()
    {
        const uint seed = 12345;
        const uint compareSeed = 99999;
        var initialWorld = CreateTestInitialWorld();
        var runnerA = new DeterministicSimulationRunner(initialWorld, _config, seed);
        var runnerB = new DeterministicSimulationRunner(initialWorld, _config, compareSeed);
        for(int tick = 0; tick < ValidationTicks; tick++)
        {
            InputFrame localInput = CreateMoveAndFireInput(tick);
            InputFrame remoteInput = CreateMoveAndFireInput(tick);
            FrameInputBundle bundle = new FrameInputBundle(tick, localInput, remoteInput);
            runnerA.Step(bundle);
            runnerB.Step(bundle);
            if(runnerA.CurrentHash != runnerB.CurrentHash)
            {
                LogResult(new ValidationResult
                {
                    TestName = "不同种子测试",
                    Passed = true,
                    Ticks = tick + 1,
                    Seed = seed,
                    FinalHash = runnerA.CurrentHash,
                    MismatchTick = tick
                });
                return;
            }
        }
        LogResult(new ValidationResult
        {
            TestName = "不同种子测试",
            Passed = false,
            Ticks = ValidationTicks,
            Seed = seed,
            FinalHash = runnerA.CurrentHash,
            MismatchTick = -1
        });
    }

    private void ReplayRecordedRunShouldMatch()
    {
        ReplayRecorder recorder = new ReplayRecorder();
        uint seed = 54321;
        recorder.BeginRecording(_config, seed);
        DeterministicSimulationRunner runner = new DeterministicSimulationRunner(CreateTestInitialWorld(), _config, seed);
        for (int tick = 0; tick < ValidationTicks; tick++)
        {
            InputFrame localInput = CreateMoveAndFireInput(tick);
            InputFrame remoteInput = CreateNeutralInput(tick);
            FrameInputBundle bundle = new FrameInputBundle(tick, localInput, remoteInput);

            runner.Step(bundle);

            recorder.RecordFrame(tick, bundle, runner.CurrentHash);
        }
        ReplayData replayData = recorder.EndRecording();
        
        if(replayData == null || replayData.Frames == null)
        {
            LogResult(new ValidationResult
            {
                TestName = "Replay 测试",
                Passed = false,
                Ticks = 0,
                Seed = seed,
                FinalHash = runner.CurrentHash,
                MismatchTick = -1
            });
            return;
        }
        if(replayData.Frames.Count != ValidationTicks)
        {
            LogResult(new ValidationResult
            {
                TestName = "Replay 测试",
                Passed = false,
                Ticks = replayData.Frames.Count,
                Seed = seed,
                FinalHash = runner.CurrentHash,
                MismatchTick = -1
            });
            return;
        }
        DeterministicSimulationRunner replayRunner = new DeterministicSimulationRunner(CreateTestInitialWorld(), _config, seed);
        
        for(int i = 0; i < replayData.Frames.Count; i++)
        {
            var frameData = replayData.Frames[i];
            replayRunner.Step(frameData.Input);
            if(replayRunner.CurrentHash != frameData.WorldHash)
            {
                LogResult(new ValidationResult
                {
                    TestName = "Replay 测试",
                    Passed = false,
                    Ticks = i + 1,
                    Seed = seed,
                    FinalHash = replayRunner.CurrentHash,
                    MismatchTick = frameData.Tick
                });
                return;
            }
        }
        LogResult(new ValidationResult
        {
            TestName = "Replay 测试",
            Passed = true,
            Ticks = ValidationTicks,
            Seed = seed,
            FinalHash = replayRunner.CurrentHash,
            MismatchTick = -1
        });
    }

    private void LogResult(ValidationResult result)
    {
        string message = $"[DeterminismValidation] Test={result.TestName}, Passed={result.Passed}, Ticks={result.Ticks}, Seed={result.Seed}, FinalHash={result.FinalHash}, MismatchTick={result.MismatchTick}";
        if(result.Passed)
        {
            Debug.Log(message);
        }
        else
        {
            Debug.LogError(message);
        }
    }

    private static InputFrame CreateNeutralInput(int tick)
    {
        return new InputFrame(tick, 0, 0, 0, 1, false);
    }

    private SimulationConfig CreateTestConfig()
    {
        return new SimulationConfig
        (
            60,
            (Fix64)5,
            10,
            (Fix64)4,
            (Fix64)100,
            1,
            (Fix64)10,
            (Fix64)0.2f,
            (Fix64)180,
            120,
            60,
            new FixVector2((Fix64)(-10), (Fix64)(-10)),
            new FixVector2((Fix64)10, (Fix64)10),
            60,
            (Fix64)1,
            (Fix64)1,
            60,
            (Fix64)1,
            (Fix64)1,
            60
        );
    }

    private WorldSnapshot CreateTestInitialWorld()
    {
        PlayerSimState player1 = new PlayerSimState(
            1,
            new FixVector3(),
            new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero),
            (Fix64)100,
            (Fix64)0.5,
            true,
            0,
            0,
            0);
            
        PlayerSimState player2 = new PlayerSimState(
            2,
            new FixVector3((Fix64)2, Fix64.Zero, Fix64.Zero),
            new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero),
            (Fix64)100,
            (Fix64)0.5,
            true,
            0,
            0,
            0);
        PlayerSimState[] players = new PlayerSimState[] { player1, player2 };
        EnemySimState enemySimState = new EnemySimState(
                0,
                new FixVector3(Fix64.One, Fix64.Zero, Fix64.Zero),
                new FixVector3(Fix64.One, Fix64.Zero, Fix64.Zero),
                (Fix64)10,
                (Fix64)10,
                (Fix64)1,
                _config.EnemyMoveSpeed,
                0,
                0);
        var world = new WorldSnapshot(0, _config, players, new BulletSimState[0], new EnemySimState[] { enemySimState });
        return world;
    }

    private InputFrame CreateMoveAndFireInput(int tick)
    {
        sbyte moveX = 0;
        sbyte moveY = 0;

        if (tick < 120)
        {
            moveX = 1;
        }
        else if (tick < 240)
        {
            moveY = 1;
        }
        else if (tick < 360)
        {
            moveX = -1;
        }

        bool fire = tick % 15 == 0;

        return new InputFrame(
            tick,
            moveX,
            moveY,
            aimX: 0,
            aimY: 1,
            fire
        );
    }
    
}
