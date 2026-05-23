using UnityEngine;
using BulletHell.Simulation.Core;
using System;
using System.Collections.Generic;

public enum SimulationRunMode
{
    Live,
    Replay
}

public class SimulationDriver : MonoBehaviour
{
    // 回放输入相对当前 tick 的预读取提前量
    private const int ReplayInputDelayTick = 2;

    // P1 输入写入调度时附加的固定延迟 tick 数，网络确认模式下会设为 0。
    private const int LocalInputDelayTicks = 2;

    public static SimulationDriver Instance { get; private set; }

    public static event System.Action<int, int> OnPlayerHpChanged;
    public static event System.Action<int, int> OnPlayerSpawned;
    public static event System.Action<int> OnPlayerRespawnCountDownChanged;
    public static event System.Action<int, ulong> OnNetworkHashSampled;

    [Header("Config")]
    // 默认使用的模拟配置资源
    [SerializeField] private SimulationConfigAsset defaultConfigAsset;

    // 当前运行时使用的模拟配置
    private SimulationConfig config;

    // 当前模拟运行模式
    private SimulationRunMode _runMode = SimulationRunMode.Live;

    // 当前是否由网络对战模式驱动实时模拟
    private bool _isNetworkLiveMode = false;

    // 本机玩家在玩家数组中的位置
    private int _localPlayerSlot = 0;

    // 最后收到网络确认的 tick
    private int _lastConfirmedNetworkTick = -1;

    // P1 输入源
    private IInputSource _p1InputSource;

    // P2 输入源
    private IInputSource _p2InputSource;

    // P1 与 P2 输入帧的双路缓冲区
    private DualInputBuffer _dualInputBuffer;

    // 输入采集、填充与消费的调度器
    private DualInputScheduler _inputScheduler;

    // 本地输入写入调度延迟 tick 数
    private int _currentLocalInputDelayTicks = LocalInputDelayTicks;

    // 最近一次等待输入的 tick
    private int _lastInputWaitTick = -1;

    // 最近一次等待输入的状态
    private InputReadyState _lastInputWaitState = InputReadyState.Ready;

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

    // 玩家视图映射使用的槽位 ID，对应服务端 playerId：P1=0，P2=1。
    private int _p1PlayerID = 0;
    private int _p2PlayerID = 1;

    // 随机种子
    private uint _seed = 0;

    // 场景初始时的玩家位置
    private Dictionary<int, Vector3> _initialPlayerPosition = new Dictionary<int, Vector3>();

    // 场景初始时缓存的敌人对象列表
    private List<EnemyBase> _initialSceneEnemies = new List<EnemyBase>();

    // 场景初始时各敌人对象对应的位置缓存
    private Dictionary<EnemyBase, Vector3> _initialEnemyPositions = new Dictionary<EnemyBase, Vector3>();

    public SimulationRunMode RunMode => _runMode;

    public bool IsReplayRunning => _runMode == SimulationRunMode.Replay;

    public int CurrentTick => _runner != null ? _runner.Tick : -1;

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
        ViewSyncManager.Instance.SetPlayerView(_p1PlayerID);
        ViewSyncManager.Instance.SetPlayerView(_p2PlayerID);
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
        int maxStepsThisUpdate = GetMaxSimulationStepsThisUpdate();
        int stepsTaken = 0;

        while (_accumulator >= tickInterval && stepsTaken < maxStepsThisUpdate)
        {
            _inputScheduler.Fill(_runner.Tick, _runMode);
            InputReadyState readyState = _inputScheduler.GetReadyState(_runner.Tick, _runMode);

            if(readyState != InputReadyState.Ready)
            {
                if (_runMode == SimulationRunMode.Replay && _runner.Tick > _inputScheduler.GetRecordMaxTick())
                {
                    Debug.Log("SimulationDriver: Replay Finished.");
                    StopReplayAndReturnToLive();
                    break;
                }

                HandleInputNotReady(_runner.Tick, readyState);
                break;
            }
            if (!_inputScheduler.TryConsume(_runner.Tick, _runMode, out InputFrame p1Input, out InputFrame p2Input))
            {
                if (_runMode == SimulationRunMode.Replay && _runner.Tick > _inputScheduler.GetRecordMaxTick())
                {
                    Debug.Log("SimulationDriver: Replay Finished.");
                    StopReplayAndReturnToLive();
                    break;
                }
                Debug.LogError($"SimulationDriver: Failed to get input for tick {_runner.Tick}.");
                break;
            }

            ClearInputWaitState();

            FrameInputBundle inputBundle = new FrameInputBundle(
                _runner.Tick,
                p1Input,
                p2Input
            );
            ProcessReplayFrameResult(inputBundle, _runner.CurrentWorld);
            _runner.Step(inputBundle);
            if (_isNetworkLiveMode && _runner.Tick % 60 == 0)
            {
                int bufferedTicks = _lastConfirmedNetworkTick - _runner.Tick;
                Debug.Log($"[NetSync] Tick={_runner.Tick} Confirmed={_lastConfirmedNetworkTick} Buffered={bufferedTicks} Hash={_runner.CurrentHash}");
                OnNetworkHashSampled?.Invoke(_runner.Tick, _runner.CurrentHash);
            }
            _accumulator -= tickInterval;
            ResolveEnemyDied();
            stepsTaken++;
        }

        WorldSnapshot newWorld = _runner.CurrentWorld;
        ViewSyncManager viewSyncManager = ViewSyncManager.Instance;
        PlayerViewSyncResult playerSyncResult = default;
        int localPlayerIndex = Mathf.Clamp(_localPlayerSlot, 0, newWorld.Players.Length - 1);
        // 同步所有玩家视图，仅为本地玩家触发 HUD 事件
        for(int i = 0; i < newWorld.Players.Length; i++)
        {
            PlayerViewSyncResult syncResult = viewSyncManager.SyncPlayerView(oldWorld.Players[i], newWorld.Players[i], GetPlayerViewIdByIndex(i));
            if(i == localPlayerIndex)
            {
                playerSyncResult = syncResult;
            }
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

    // 触发敌人死亡事件
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

        int localPlayerIndex = Mathf.Clamp(_localPlayerSlot, 0, _runner.CurrentWorld.Players.Length - 1);
        PlayerSimState localPlayer = _runner.CurrentWorld.Players[localPlayerIndex];
        currentHp = (int)localPlayer.Hp;
        maxHp = (int)config.PlayerMaxHp;
        respawnCountdownTicks = localPlayer.RespawnCountdownTicks;
        return true;
    }

    public void SetLiveMode(PlayerController playerController = null)
    {
        // playerController 参数为 null 时，从当前场景查找
        // 这里不再依赖 _playerViews，因为视图管理已交给 ViewSyncManager

        if (playerController == null)
        {
            playerController = ViewSyncManager.Instance.GetPlayerView(_p1PlayerID);
            if(playerController == null)
            {
                Debug.LogError("SimulationDriver: PlayerController was not found when switching to live mode.");
                _p1InputSource = null;
                _p2InputSource = null;
                return;
            }
        }
        _p1InputSource = new LiveInputSource(playerController);
        _p2InputSource = new MockRemoteInputSource();
        _runMode = SimulationRunMode.Live;
        _verifier = null;
        _recorder = new ReplayRecorder();
        _seed = 1;
        _recorder.BeginRecording(config, _seed);
        _currentLocalInputDelayTicks = LocalInputDelayTicks;
        _isNetworkLiveMode = false;
        _localPlayerSlot = 0;
        ResetSimulationWorld();
    }

    public void SetNetworkLiveMode(uint seed, RemoteInputQueueSource p1InputSource, RemoteInputQueueSource p2InputSource, int localPlayerSlot = 0)
    {
        if(p1InputSource == null)
        {
            Debug.LogError("SimulationDriver: p1InputSource 为空.");
            _p1InputSource = null;
            _p2InputSource = null;
            return;
        }
        if(p2InputSource == null)
        {
            Debug.LogError("SimulationDriver: p2InputSource 为空.");
            _p1InputSource = null;
            _p2InputSource = null;
            return;
        }

        _p1InputSource = p1InputSource;
        _p2InputSource = p2InputSource;
        _runMode = SimulationRunMode.Live;
        _verifier = null;
        _recorder = new ReplayRecorder();
        _seed = seed;
        _currentLocalInputDelayTicks = 0; // 网络模式下 P1 输入不额外添加延迟
        _isNetworkLiveMode = true;
        _localPlayerSlot = Mathf.Clamp(localPlayerSlot, 0, 1);
        _lastConfirmedNetworkTick = -1;
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


        ViewSyncManager.Instance.ResetSceneView(_p1PlayerID, _initialPlayerPosition, _initialSceneEnemies);
        _seed = replayData.Seed;

        _p1InputSource = new ReplayInputSource(replayData);
        _p2InputSource = new MockRemoteInputSource();
        _runMode = SimulationRunMode.Replay;
        _isNetworkLiveMode = false;
        _localPlayerSlot = 0;
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
        
        PlayerController localPlayer = viewSyncManager.GetPlayerView(_p1PlayerID);
        if(localPlayer == null)
        {
            Debug.LogError("SimulationDriver: PlayerController is null.");
            return;
        }
        _initialPlayerPosition[_p1PlayerID] = localPlayer.transform.position;

        PlayerController remotePlayer = viewSyncManager.GetPlayerView(_p2PlayerID);
        if(remotePlayer == null)
        {
            Debug.LogError("SimulationDriver: PlayerController is null.");
            return;
        }
        _initialPlayerPosition[_p2PlayerID] = remotePlayer.transform.position;


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
            _p1InputSource,
            _p2InputSource,
            _currentLocalInputDelayTicks,
            ReplayInputDelayTick
        );

        ViewSyncManager.Instance?.ClearBulletViews();
        ViewSyncManager.Instance?.RestoreSceneActors(_initialPlayerPosition,
        _initialSceneEnemies, _initialEnemyPositions);

        _initialPlayerPosition.TryGetValue(_p1PlayerID, out Vector3 localPlayerPos);
        // 模拟层初始化
        PlayerSimState player1 = new PlayerSimState(
            0,
            new FixVector3((Fix64)localPlayerPos.x, (Fix64)localPlayerPos.y, (Fix64)localPlayerPos.z),
            new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero),
            config.PlayerMaxHp,
            config.PlayerHitRadius,
            true,
            0,
            0,
            0);
            
        _initialPlayerPosition.TryGetValue(_p2PlayerID, out Vector3 remotePlayerPos);
        PlayerSimState player2 = new PlayerSimState(
            1,
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

    public void PushP2Input(InputFrame inputFrame)
    {
        if (_p2InputSource is RemoteInputQueueSource p2InputQueueSource)
        {
            p2InputQueueSource.PushInput(inputFrame);
        }
    }

    private int GetPlayerViewIdByIndex(int playerIndex)
    {
        return playerIndex == 0 ? _p1PlayerID : _p2PlayerID;
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

    private void HandleInputNotReady(int tick, InputReadyState readyState)
    {
        if (_lastInputWaitTick == tick && _lastInputWaitState == readyState)
        {
            return;
        }
    
        _lastInputWaitTick = tick;
        _lastInputWaitState = readyState;
    
    }

    public void SetLastConfirmedNetworkTick(int tick)
    {
        if(tick > _lastConfirmedNetworkTick)
        {
            _lastConfirmedNetworkTick = tick;
        }
    }

    private int GetMaxSimulationStepsThisUpdate()
    {
        if (!_isNetworkLiveMode)
        {
            return 4; // 或保持原来的无限 while，先给个上限更安全
        }

        int bufferedTicks = _lastConfirmedNetworkTick - _runner.Tick;

        if (bufferedTicks >= 6)
        {
            return 4;
        }

        if (bufferedTicks >= 3)
        {
            return 2;
        }

        return 1;
    }

    private void ClearInputWaitState()
    {
        _lastInputWaitTick = -1;
        _lastInputWaitState = InputReadyState.Ready;

    }

    // 获取 Transform 路径
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
