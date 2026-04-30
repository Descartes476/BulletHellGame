using UnityEngine;
using BulletHell.Simulation.Core;
using System;
using System.Collections.Generic;
using System.Linq;

public enum SimulationRunMode
{
    Live,
    Replay
}

public class SimulationDriver : MonoBehaviour
{
    // 默认使用的模拟配置资源
    [SerializeField] private SimulationConfigAsset defaultConfigAsset;

    public static SimulationDriver Instance { get; private set; }

    // 当前运行时使用的模拟配置
    private SimulationConfig config;
    // 当前模拟驱动使用的输入源
    private IInputSource _inputSource;
    // 当前模拟运行模式
    private SimulationRunMode _runMode = SimulationRunMode.Live;
    // 统一的确定性仿真推进器
    private DeterministicSimulationRunner _runner;
    // 实时模式下用于录制回放数据的记录器
    private ReplayRecorder _recorder;
    // 回放模式下用于校验世界状态的校验器
    private ReplayVerifier _verifier;
    // 帧时间累积器，用于按固定tick推进模拟
    private float _accumulator;
    // 下一个可分配的敌人实体ID
    private int _nextEnemyEntityID = 1;
    // 玩家实体使用的固定ID
    private int _playerID = 1;
    // 随机种子
    private uint _seed = 0;
    // 场景初始时的玩家位置
    private Vector3 _initialPlayerPosition;
    // 场景初始时缓存的敌人对象列表
    private List<EnemyBase> _initialSceneEnemies = new List<EnemyBase>();
    // 场景初始时各敌人对象对应的位置缓存
    private Dictionary<EnemyBase, Vector3> _initialEnemyPositions = new Dictionary<EnemyBase, Vector3>();

    // 视图同步管理器
    private ViewSyncManager _viewSyncManager;

    public static event System.Action<int, int> OnPlayerHpChanged;
    public static event System.Action<int, int> OnPlayerSpawned;
    public static event System.Action<int> OnPlayerRespawnCountDownChanged;

    public SimulationRunMode RunMode => _runMode;
    public bool IsReplayRunning => _runMode == SimulationRunMode.Replay;

    void Start()
    {
        Instance = this;
        _accumulator = 0f;

        if (defaultConfigAsset == null)
        {
            Debug.LogError("SimulationDriver: Default config asset is not assigned.");
            enabled = false;
            return;
        }

        PlayerController playerController = FindObjectOfType<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("SimulationDriver: PlayerController was not found.");
            enabled = false;
            return;
        }

        config = defaultConfigAsset.ToSimulationConfig();
        _viewSyncManager = new ViewSyncManager(config);
        _viewSyncManager.SetPlayerView(_playerID, playerController);
        playerController.SetSimulationDriven(true);
        CacheInitialSceneState(playerController); // 记录场景初始状态
        SetLiveMode(playerController);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (config.TickRate <= 0)
            return;

        if (_runner == null)
            return;

        WorldSnapshot oldWorld = _runner.CurrentWorld;

        float tickInterval = 1f / config.TickRate;
        _accumulator += Time.deltaTime;

        while (_accumulator >= tickInterval)
        {
            InputFrame inputFrame;
            if (!TryGetCurrentInput(out inputFrame))
            {
                if (_runMode == SimulationRunMode.Replay && _runner.Tick > _inputSource.GetRecordMaxTick())
                {
                    Debug.Log("SimulationDriver: Replay Finished.");
                    StopReplayAndReturnToLive();
                    return;
                }
                Debug.LogError($"SimulationDriver: Failed to get input for tick {_runner.Tick}.");
                return;
            }
            ProcessReplayFrameResult(inputFrame, _runner.CurrentWorld);
            _runner.Step(inputFrame);

            _accumulator -= tickInterval;
            ResolveEnemyDied();
        }

        WorldSnapshot newWorld = _runner.CurrentWorld;
        var playerSyncResult = _viewSyncManager.SyncPlayerView(oldWorld.Player, newWorld.Player, _playerID);
        _viewSyncManager.SyncBulletViews(newWorld.Bullets);
        _viewSyncManager.SyncEnemyViews(newWorld.Enemies);
        
        // 触发玩家相关事件
        if (playerSyncResult.HpChanged)
        {
            OnPlayerHpChanged?.Invoke(playerSyncResult.CurrentHp, playerSyncResult.MaxHp);
        }
        if (playerSyncResult.PlayerSpawned)
        {
            OnPlayerSpawned?.Invoke(playerSyncResult.CurrentHp, playerSyncResult.MaxHp);
        }
        if (playerSyncResult.RespawnCountdownChanged)
        {
            OnPlayerRespawnCountDownChanged?.Invoke(playerSyncResult.RespawnCountdownTicks);
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (_recorder != null)
            {
                ReplayData replayData = _recorder.EndRecording();
                TryStartReplay(replayData);
            }
            else
            {
                Debug.Log("SimulationDriver: _recorder为空.");
            }
        }
    }

    //触发敌人死亡事件
    private void ResolveEnemyDied()
    {
        if (_runner == null)
        {
            return;
        }

        foreach (var enemyId in _runner.EnemyDiedEntityIds)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerEnemyDied(enemyId);
            }
        }
    }


    public bool TryGetPlayerHudState(out int currentHp, out int maxHp, out int respawnCountdownTicks)
    {
        if (_runner == null)
        {
            currentHp = 0;
            maxHp = 0;
            respawnCountdownTicks = 0;
            return false;
        }

        currentHp = (int)_runner.CurrentWorld.Player.Hp;
        maxHp = (int)config.PlayerMaxHp;
        respawnCountdownTicks = _runner.CurrentWorld.Player.RespawnCountdownTicks;
        return true;
    }

    private bool TryGetCurrentInput(out InputFrame inputFrame)
    {
        if (_inputSource == null)
        {
            inputFrame = default;
            return false;
        }

        if (_runner == null)
        {
            inputFrame = default;
            return false;
        }

        return _inputSource.TryGetInput(_runner.Tick, out inputFrame);
    }

    public void SetLiveMode(PlayerController playerController = null)
    {
        // playerController 参数为 null 时，从当前场景查找
        // 这里不再依赖 _playerViews，因为视图管理已交给 ViewSyncManager

        if (playerController == null)
        {
            playerController = _viewSyncManager.GetPlayerView(_playerID);
            if(playerController == null)
            {
                Debug.LogError("SimulationDriver: PlayerController was not found when switching to live mode.");
                _inputSource = null;
                return;
            }
        }
        _inputSource = new LiveInputSource(playerController);
        _runMode = SimulationRunMode.Live;
        _verifier = null;
        _recorder = new ReplayRecorder();
        _seed = 1;
        _recorder.BeginRecording(config, _seed);
        ResetSimulationWorld();
    }

    public bool SetReplayMode(ReplayData replayData)
    {
        if (replayData == null)
        {
            Debug.LogError("SimulationDriver: ReplayData is null.");
            return false;
        }

        if (!IsReplayConfigCompatible(replayData))
        {
            return false;
        }


        _viewSyncManager.ResetSceneView(_playerID, _initialPlayerPosition, _initialSceneEnemies);
        _seed = replayData.Seed;
        ResetSimulationWorld();

        _inputSource = new ReplayInputSource(replayData);
        _runMode = SimulationRunMode.Replay;
        _recorder = null;
        _verifier = new ReplayVerifier();
        _verifier.Load(replayData);
        return true;
    }

    // 执行帧记录/回放校验
    private void ProcessReplayFrameResult(InputFrame inputFrame, WorldSnapshot world)
    {
        ulong worldHash = WorldStateHasher.Compute(world);
        // 记录模式，记录当前世界状态
        if (_runMode == SimulationRunMode.Live)
        {
            _recorder?.RecordFrame(world.Tick, inputFrame, worldHash);
            return;
        }
        // 回放模式，校验当前世界状态
        if (_runMode == SimulationRunMode.Replay && _verifier != null)
        {
            if (!_verifier.Verify(world.Tick, worldHash, out ReplayMismatchInfo mismatchInfo))
            {
                Debug.LogError($"SimulationDriver: Replay mismatch at tick {mismatchInfo.Tick}, expected hash {mismatchInfo.ExpectedHash}, actual hash {mismatchInfo.ActualHash}.");
            }
        }
    }

    // 当前配置与记录配置校验
    private bool IsReplayConfigCompatible(ReplayData replayData)
    {
        ReplayConfigSnapshot snapshot = replayData.ConfigSnapshot;

        if (snapshot.TickRate != config.TickRate)
        {
            Debug.LogError($"SimulationDriver: Replay TickRate mismatch. Replay={snapshot.TickRate}, Current={config.TickRate}.");
            return false;
        }

        if (snapshot.PlayerMoveSpeedRaw != config.PlayerMoveSpeed.RawValue)
        {
            Debug.LogError($"SimulationDriver: Replay PlayerMoveSpeed mismatch. Replay={snapshot.PlayerMoveSpeedRaw}, Current={config.PlayerMoveSpeed.RawValue}.");
            return false;
        }

        if (snapshot.PlayerBulletSpeedRaw != config.PlayerBulletSpeed.RawValue)
        {
            Debug.LogError($"SimulationDriver: Replay PlayerBulletSpeed mismatch. Replay={snapshot.PlayerBulletSpeedRaw}, Current={config.PlayerBulletSpeed.RawValue}.");
            return false;
        }

        if (snapshot.EnemyBulletSpeedRaw != config.EnemyBulletSpeed.RawValue)
        {
            Debug.LogError($"SimulationDriver: Replay EnemyBulletSpeed mismatch. Replay={snapshot.EnemyBulletSpeedRaw}, Current={config.EnemyBulletSpeed.RawValue}.");
            return false;
        }

        if (snapshot.PlayerMaxHpRaw != config.PlayerMaxHp.RawValue)
        {
            Debug.LogError($"SimulationDriver: Replay PlayerMaxHp mismatch. Replay={snapshot.PlayerMaxHpRaw}, Current={config.PlayerMaxHp.RawValue}.");
            return false;
        }

        if (snapshot.PlayerRespawnTicks != config.PlayerRespawnTicks)
        {
            Debug.LogError($"SimulationDriver: Replay PlayerRespawnTicks mismatch. Replay={snapshot.PlayerRespawnTicks}, Current={config.PlayerRespawnTicks}.");
            return false;
        }

        if (snapshot.PlayerInvincibleTicks != config.PlayerInvincibleTicks)
        {
            Debug.LogError($"SimulationDriver: Replay PlayerInvincibleTicks mismatch. Replay={snapshot.PlayerInvincibleTicks}, Current={config.PlayerInvincibleTicks}.");
            return false;
        }

        return true;
    }

    // 记录场景初始状态
    private void CacheInitialSceneState(PlayerController playerController)
    {
        _initialPlayerPosition = playerController.transform.position;
        _initialSceneEnemies.Clear();
        _initialEnemyPositions.Clear();

        EnemyBase[] sceneEnemies = FindObjectsOfType<EnemyBase>();
        Array.Sort(sceneEnemies, (a, b) =>
        {
            int pathCompare = string.CompareOrdinal(GetTransformPath(a.transform), GetTransformPath(b.transform));
            if (pathCompare != 0)
            {
                return pathCompare;
            }

            return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
        });
        for (int i = 0; i < sceneEnemies.Length; i++)
        {
            EnemyBase enemy = sceneEnemies[i];
            _initialSceneEnemies.Add(enemy);
            _initialEnemyPositions[enemy] = enemy.transform.position;
        }
    }

    private void ResetSimulationWorld()
    {
        _accumulator = 0f;
        _nextEnemyEntityID = 1;

        _viewSyncManager?.ClearBulletViews();
        _viewSyncManager?.RestoreSceneActors(_playerID, _initialPlayerPosition,
        _initialSceneEnemies, _initialEnemyPositions); 

        // 模拟层初始化
        PlayerSimState initialPlayer = new PlayerSimState(
            _playerID,
            new FixVector3((Fix64)_initialPlayerPosition.x, (Fix64)_initialPlayerPosition.y, (Fix64)_initialPlayerPosition.z),
            new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero),
            config.PlayerMaxHp,
            config.PlayerHitRadius,
            true,
            0,
            0,
            0);
        var enemies = GetEnemySimStates();
        WorldSnapshot initialWorld = new WorldSnapshot(0, config, initialPlayer, new BulletSimState[0], enemies);
        _runner = new DeterministicSimulationRunner(initialWorld, config, _seed);
        OnPlayerSpawned?.Invoke((int)initialPlayer.Hp, (int)config.PlayerMaxHp);
    }

    public bool TryStartReplay(ReplayData replaydata)
    {
        if (_runMode == SimulationRunMode.Live)
        {
            return SetReplayMode(replaydata);
        }
        return false;
    }

    public void StopReplayAndReturnToLive()
    {
        SetLiveMode();
    }

    private EnemySimState[] GetEnemySimStates()
    {
        List<EnemySimState> enemySimStates = new List<EnemySimState>();
        Dictionary<int, EnemyBase> enemyViews = new Dictionary<int, EnemyBase>();
        for (int i = 0; i < _initialSceneEnemies.Count; i++)
        {
            EnemyBase enemy = _initialSceneEnemies[i];
            if (enemy == null)
            {
                continue;
            }
            Vector3 pos = enemy.transform.position;
            FixVector3 fixPos = new FixVector3((Fix64)pos.x, (Fix64)pos.y, (Fix64)pos.z);
            int enemyEntityId = _nextEnemyEntityID;
            EnemySimState enemySimState = new EnemySimState(
                enemyEntityId,
                fixPos,
                new FixVector3(Fix64.One, Fix64.Zero, Fix64.Zero),
                (Fix64)enemy.MaxHp,
                (Fix64)enemy.MaxHp,
                (Fix64)enemy.HitRadius,
                config.EnemyMoveSpeed,
                0,
                0);
            enemySimStates.Add(enemySimState);
            enemyViews[enemyEntityId] = enemy;
            _nextEnemyEntityID++;
        }
        
        // 设置敌人视图映射到 ViewSyncManager
        _viewSyncManager?.SetEnemyViews(enemyViews);
        return enemySimStates.ToArray();
    }

    //获取Transform路径
    private static string GetTransformPath(Transform current)
    {
        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}
