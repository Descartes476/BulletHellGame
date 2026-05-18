// using UnityEngine;
// using BulletHell.Simulation.Core;
// using System.Collections.Generic;

// public class DualSimulationVerifierTest : MonoBehaviour
// {
//     [Header("测试配置")]
//     [SerializeField] private bool runTestsOnStart = true;
//     [SerializeField] private int testFrameCount = 20;
    
//     private LocalDualSimulationVerifier _verifier = new LocalDualSimulationVerifier();
    
//     void Start()
//     {
//         if (runTestsOnStart)
//         {
//             RunAllTests();
//         }
//     }
    
//     [ContextMenu("运行所有测试")]
//     public void RunAllTests()
//     {
//         Debug.Log("=== 开始双模拟验证测试 ===");
        
//         TestEmptyReplay();
//         TestSingleFrameReplay();
//         TestNormalReplay();
//         TestInconsistentInitialWorld();
//         TestManualLogicInconsistency();
        
//         Debug.Log("=== 双模拟验证测试完成 ===");
//     }
    
//     private void TestEmptyReplay()
//     {
//         Debug.Log("测试 1: 空 replay");
        
//         var initialWorld = CreateTestInitialWorld();
//         var config = new SimulationConfig();
//         var emptyReplay = new ReplayData
//         {
//             TickRate = 60,
//             Seed = 12345,
//             ConfigSnapshot = new ReplayConfigSnapshot(),
//             Frames = new List<ReplayFrame>()
//         };
        
//         var result = _verifier.Verify(emptyReplay, initialWorld, config);
        
//         if (result.IsMatch)
//         {
//             Debug.Log("✅ 空 replay 测试通过");
//         }
//         else
//         {
//             Debug.LogError($"❌ 空 replay 测试失败: {result.ErrorMessage}");
//         }
//     }
    
//     private void TestSingleFrameReplay()
//     {
//         Debug.Log("测试 2: 单帧 replay");
        
//         var initialWorld = CreateTestInitialWorld();
//         var config = new SimulationConfig();
//         var singleFrameReplay = CreateTestReplay(1);
        
//         var result = _verifier.Verify(singleFrameReplay, initialWorld, config);
        
//         if (result.IsMatch)
//         {
//             Debug.Log("✅ 单帧 replay 测试通过");
//         }
//         else
//         {
//             Debug.LogError($"❌ 单帧 replay 测试失败: {result.ErrorMessage}");
//         }
//     }
    
//     private void TestNormalReplay()
//     {
//         Debug.Log("测试 3: 正常多帧 replay");
        
//         var initialWorld = CreateTestInitialWorld();
//         var config = new SimulationConfig();
//         var normalReplay = CreateTestReplay(testFrameCount);
        
//         var result = _verifier.Verify(normalReplay, initialWorld, config);
        
//         if (result.IsMatch)
//         {
//             Debug.Log($"✅ 正常 {testFrameCount} 帧 replay 测试通过");
//         }
//         else
//         {
//             Debug.LogError($"❌ 正常 replay 测试失败: {result.ErrorMessage}");
//         }
//     }
    
//     private void TestInconsistentInitialWorld()
//     {
//         Debug.Log("测试 4: 初始世界不一致");
        
//         var config = new SimulationConfig();
//         var worldA = CreateTestInitialWorld();
//         var worldB = CreateTestInitialWorld();
        
//         // 故意让两个世界不同
//         var playerB = new PlayerSimState(
//             1, // 不同的 entityId
//             new FixVector3(Fix64.One, Fix64.Zero, Fix64.Zero), // 不同的位置
//             worldB.Player.AimDirection,
//             worldB.Player.Hp,
//             worldB.Player.HitRadius,
//             worldB.Player.IsAlive,
//             worldB.Player.FireCooldownTicks,
//             worldB.Player.RespawnCountdownTicks,
//             worldB.Player.InvincibleTicks
//         );
        
//         worldB = new WorldSnapshot(
//             worldB.Tick,
//             worldB.Config,
//             playerB,
//             worldB.Bullets,
//             worldB.Enemies
//         );
        
//         var emptyReplay = new ReplayData
//         {
//             TickRate = 60,
//             Seed = 12345,
//             ConfigSnapshot = new ReplayConfigSnapshot(),
//             Frames = new List<ReplayFrame>()
//         };
        
//         var result = _verifier.Verify(emptyReplay, worldA, config);
        
//         // 这里我们需要手动创建第二个 runner 来模拟不一致
//         var runnerA = new DeterministicSimulationRunner(worldA, config, 0);
//         var runnerB = new DeterministicSimulationRunner(worldB, config, 0);
        
//         if (runnerA.CurrentHash != runnerB.CurrentHash)
//         {
//             Debug.Log("✅ 初始世界不一致测试通过");
//         }
//         else
//         {
//             Debug.LogError("❌ 初始世界不一致测试失败：应该检测到不一致");
//         }
//     }
    
//     private void TestManualLogicInconsistency()
//     {
//         Debug.Log("测试 5: 手动制造逻辑不一致");
        
//         var initialWorld = CreateTestInitialWorld();
//         var config = new SimulationConfig();
//         var replay = CreateTestReplay(5);
        
//         Debug.Log("⚠️ 请手动在 DeterministicSimulationRunner 中制造不一致，然后重新运行此测试");
//         Debug.Log("例如：在 ResolveInput 方法中添加 if (inputFrame.Tick == 3) { /* 改变逻辑 */ }");
        
//         var result = _verifier.Verify(replay, initialWorld, config);
        
//         // 如果没有手动制造不一致，这个测试应该通过
//         if (result.IsMatch)
//         {
//             Debug.Log("✅ 逻辑一致性测试通过（未检测到不一致）");
//         }
//         else
//         {
//             Debug.Log($"✅ 逻辑不一致测试通过，在 Tick {result.MismatchTick} 检测到不一致");
//         }
//     }
    
//     private WorldSnapshot CreateTestInitialWorld()
//     {
//         var config = new SimulationConfig();
//         var player = new PlayerSimState(
//             0,
//             new FixVector3(Fix64.Zero, Fix64.Zero, Fix64.Zero),
//             new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero),
//             (Fix64)100,
//             (Fix64)1,
//             true,
//             0,
//             0,
//             0
//         );
        
//         var enemies = new EnemySimState[0];
//         var bullets = new BulletSimState[0];
        
//         return new WorldSnapshot(0, config, player, bullets, enemies);
//     }
    
//     private ReplayData CreateTestReplay(int frameCount)
//     {
//         var replay = new ReplayData
//         {
//             TickRate = 60,
//             Seed = 12345,
//             ConfigSnapshot = new ReplayConfigSnapshot(),
//             Frames = new List<ReplayFrame>()
//         };
        
//         for (int i = 0; i < frameCount; i++)
//         {
//             var input = new InputFrame(
//                 i,
//                 0, 0,
//                 0, 1,
//                 false
//             );
            
//             var frame = new ReplayFrame
//             {
//                 Tick = i,
//                 Input = input,
//                 WorldHash = 0
//             };
            
//             replay.Frames.Add(frame);
//         }
        
//         return replay;
//     }
// }
