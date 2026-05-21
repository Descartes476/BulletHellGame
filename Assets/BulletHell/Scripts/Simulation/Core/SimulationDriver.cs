using UnityEngine;
using BulletHell.Simulation.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.tvOS;
using UnityEngine.SocialPlatforms;

public enum SimulationRunMode
{
    Live,
    Replay
}

public class SimulationDriver : MonoBehaviour
{
    // 默认使用的模拟配置资源
    [SerializeField] private SimulationConfigAsset defaultConfigAsset;

    // 输入缓冲相对当前tick的预拉取提前量
    private const int ReplayInputDelayTick = 2;
    // 本地输入写入调度时附加的固定延迟tick数
    private const int LocalInputDelayTicks = 2;
    public static SimulationDriver Instance { get; private set; }

    // 当前运行时使用的模拟配置
    private SimulationConfig config;

    // 本地玩家输入源
    private IInputSource _localInputSource;
    // 远端玩家输入源
    private IInputSource _remoteInputSource;
    // 本地与远端输入帧的双路缓冲区
    private DualInputBuffer _dualInputBuffer;
    // 输入采集、填充与消费的调度器
    private DualInputScheduler _inputScheduler;
    // 最后执行帧
    private int _lastInputWaitTick = -1;
    // 本地写入调度延迟Tick数
    private int _currentLocalInputDelayTicks = LocalInputDelayTicks;
    // 最后执行帧的状态
    private InputReadyState _lastInputWaitState = InputReadyState.Ready;
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
    private int _localPlayerID = 1;
    private int _remotePlayerID = 2;
    // 随机种子
    private uint _seed = 0;
    // 场景初始时的玩家位置
    private Dictionary<int, Vector3> _initialPlayerPosition = new Dictionary<int, Vector3>();
    // 场景初始时缓存的敌人对象列表
    private List<EnemyBase> _initialSceneEnemies = new List<EnemyBase>();
    // 场景初始时各敌人对象对应的位置缓存
    private Dictionary<EnemyBase, Vector3> _initialEnemyPositions = new Dictionary<EnemyBase, Vector3>();

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

        config = defaultConfigAsset.ToSimulationConfig();
        ViewSyncManager.Instance.SetConfig(config);
        ViewSyncManager.Instance.SetPlayerView(_localPlayerID);
        ViewSyncManager.Instance.SetPlayerView(_remotePlayerID);
        CacheInitialSceneState(); // 记录场景初始状态
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
            _inputScheduler.Fill(_runner.Tick, _runMode);
            InputReadyState readyState = _inputScheduler.GetReadyState(_runner.Tick, _runMode);

            if(readyState != InputReadyState.Ready)
            {
                if (_runMode == SimulationRunMode.Replay && _runner.Tick > _inputScheduler.GetRecordMaxTick())
                {
                    Debug.Log("SimulationDriver: Replay Finished.");
                    StopReplayAndReturnToLive();
                    return;
                }

                HandleInputNotReady(_runner.Tick, readyState);
                return;
            }
            if (!_inputScheduler.TryConsume(_runner.Tick, _runMode, out InputFrame inputFrame, out InputFrame remoteInput))
            {
                if (_runMode == SimulationRunMode.Replay && _runner.Tick > _inputScheduler.GetRecordMaxTick())
                {
                    Debug.Log("SimulationDriver: Replay Finished.");
                    StopReplayAndReturnToLive();
                    return;
                }
                Debug.LogError($"SimulationDriver: Failed to get input for tick {_runner.Tick}.");
                return;
            }

            ClearInputWaitState();

            FrameInputBundle inputBundle = new FrameInputBundle(
                _runner.Tick,
                inputFrame,
                remoteInput
            );
            ProcessReplayFrameResult(inputBundle, _runner.CurrentWorld);
            _runner.Step(inputBundle);
            Debug.Log($"执行了LocalInput Tick={inputFrame.Tick}, RemoteInput Tick={remoteInput.Tick}");
            _accumulator -= tickInterval;
            ResolveEnemyDied();
        }

        WorldSnapshot newWorld = _runner.CurrentWorld;
        ViewSyncManager viewSyncManager = ViewSyncManager.Instance;
        var playerSyncResult = viewSyncManager.SyncPlayerView(oldWorld.Player, newWorld.Player, _localPlayerID);
        // 同步玩家2（若存在），不触发本地HUD事件
        if (oldWorld.Players.Length > 1 && newWorld.Players.Length > 1)
        {
            viewSyncManager.SyncPlayerView(oldWorld.Players[1], newWorld.Players[1], _remotePlayerID);
        }
        viewSyncManager.SyncBulletViews(newWorld.Bullets);
        viewSyncManager.SyncEnemyViews(newWorld.Enemies);
        
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

    public void SetLiveMode(PlayerController playerController = null)
    {
        // playerController 参数为 null 时，从当前场景查找
        // 这里不再依赖 _playerViews，因为视图管理已交给 ViewSyncManager

        if (playerController == null)
        {
            playerController = ViewSyncManager.Instance.GetPlayerView(_localPlayerID);
            if(playerController == null)
            {
                Debug.LogError("SimulationDriver: PlayerController was not found when switching to live mode.");
                _localInputSource = null;
                _remoteInputSource = null;
                return;
            }
        }
        _localInputSource = new LiveInputSource(playerController);
        _remoteInputSource = new MockRemoteInputSource();
        _runMode = SimulationRunMode.Live;
        _verifier = null;
        _recorder = new ReplayRecorder();
        _seed = 1;
        _recorder.BeginRecording(config, _seed);
        _currentLocalInputDelayTicks = LocalInputDelayTicks;
        ResetSimulationWorld();
    }

    public void SetNetworkLiveMode(uint seed, RemoteInputQueueSource localInputSource, RemoteInputQueueSource remoteInputSource)
    {
        if(localInputSource == null)
        {
            Debug.LogError("SimulationDriver: localInputSource 为空.");
            _localInputSource = null;
            _remoteInputSource = null;
            return;
        }
        if(remoteInputSource == null)
        {
            Debug.LogError("SimulationDriver: remoteInputSource 为空.");
            _localInputSource = null;
            _remoteInputSource = null;
            return;
        }

        _localInputSource = localInputSource;
        _remoteInputSource = remoteInputSource;
        _runMode = SimulationRunMode.Live;
        _verifier = null;
        _recorder = new ReplayRecorder();
        _seed = seed;
        _recorder.BeginRecording(config, _seed);
        _currentLocalInputDelayTicks = 0; // 网络模式下本地输入不额外添加延迟
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


        ViewSyncManager.Instance.ResetSceneView(_localPlayerID, _initialPlayerPosition, _initialSceneEnemies);
        _seed = replayData.Seed;

        _localInputSource = new ReplayInputSource(replayData);
        _remoteInputSource = new MockRemoteInputSource();
        _runMode = SimulationRunMode.Replay;
        _recorder = null;
        _verifier = new ReplayVerifier();
        _verifier.Load(replayData);
        ResetSimulationWorld();
        return true;
    }

    // 执行帧记录/回放校验
    private void ProcessReplayFrameResult(FrameInputBundle inputBundle, WorldSnapshot world)
    {
        ulong worldHash = WorldStateHasher.Compute(world);
        // 记录模式，记录当前世界状态
        if (_runMode == SimulationRunMode.Live)
        {
            _recorder?.RecordFrame(world.Tick, inputBundle, worldHash);
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
    private void CacheInitialSceneState()
    {
        _initialPlayerPosition.Clear();
        _initialSceneEnemies.Clear();
        _initialEnemyPositions.Clear();

        ViewSyncManager viewSyncManager = ViewSyncManager.Instance;
        
        PlayerController localPlayer = viewSyncManager.GetPlayerView(_localPlayerID);
        if(localPlayer == null)
        {
            Debug.LogError("SimulationDriver: PlayerController is null.");
            return;
        }
        _initialPlayerPosition[_localPlayerID] = localPlayer.transform.position;

        PlayerController remotePlayer = viewSyncManager.GetPlayerView(_remotePlayerID);
        if(remotePlayer == null)
        {
            Debug.LogError("SimulationDriver: PlayerController is null.");
            return;
        }
        _initialPlayerPosition[_remotePlayerID] = remotePlayer.transform.position;


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
        _dualInputBuffer = new DualInputBuffer();
        _inputScheduler = new DualInputScheduler(
            _dualInputBuffer,
            _localInputSource,
            _remoteInputSource,
            _currentLocalInputDelayTicks,
            ReplayInputDelayTick
        );

        ViewSyncManager.Instance?.ClearBulletViews();
        ViewSyncManager.Instance?.RestoreSceneActors(_initialPlayerPosition,
        _initialSceneEnemies, _initialEnemyPositions);

        _initialPlayerPosition.TryGetValue(_localPlayerID, out Vector3 localPlayerPos);
        // 模拟层初始化
        PlayerSimState player1 = new PlayerSimState(
            1,
            new FixVector3((Fix64)localPlayerPos.x, (Fix64)localPlayerPos.y, (Fix64)localPlayerPos.z),
            new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero),
            config.PlayerMaxHp,
            config.PlayerHitRadius,
            true,
            0,
            0,
            0);
            
        _initialPlayerPosition.TryGetValue(_remotePlayerID, out Vector3 remotePlayerPos);
        PlayerSimState player2 = new PlayerSimState(
            2,
            new FixVector3((Fix64)remotePlayerPos.x, (Fix64)remotePlayerPos.y, (Fix64)remotePlayerPos.z),
            new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero),
            config.PlayerMaxHp,
            config.PlayerHitRadius,
            true,
            0,
            0,
            0
        );
        PlayerSimState[] players = {player1, player2};

        var enemies = GetEnemySimStates();
        WorldSnapshot initialWorld = new WorldSnapshot(0, config, players, new BulletSimState[0], enemies);
        _runner = new DeterministicSimulationRunner(initialWorld, config, _seed);
        if(_runMode == SimulationRunMode.Live)
        {
            _inputScheduler.WarmupLive();
        }
        OnPlayerSpawned?.Invoke((int)player1.Hp, (int)config.PlayerMaxHp);
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

    public void PushRemoteInput(InputFrame inputFrame)
    {
        if (_remoteInputSource is RemoteInputQueueSource remoteInputQueueSource)
        {
            remoteInputQueueSource.PushInput(inputFrame);
        }
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
        ViewSyncManager.Instance.SetEnemyViews(enemyViews);
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

    private void HandleInputNotReady(int tick, InputReadyState readyState)
    {
        if (_lastInputWaitTick == tick && _lastInputWaitState == readyState)
        {
            return;
        }
    
        _lastInputWaitTick = tick;
        _lastInputWaitState = readyState;
    
        Debug.Log($"SimulationDriver: Waiting input at tick {tick}, state={readyState}.");
    }

    private void ClearInputWaitState()
    {
        _lastInputWaitTick = -1;
        _lastInputWaitState = InputReadyState.Ready;

    }
}
