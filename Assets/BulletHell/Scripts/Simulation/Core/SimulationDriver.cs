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
    // 场景初始时的玩家位置
    private Vector3 _initialPlayerPosition;
    // 场景初始时缓存的敌人对象列表
    private List<EnemyBase> _initialSceneEnemies = new List<EnemyBase>();
    // 场景初始时各敌人对象对应的位置缓存
    private Dictionary<EnemyBase, Vector3> _initialEnemyPositions = new Dictionary<EnemyBase, Vector3>();

    // 子弹实体ID到显示对象的映射
    private Dictionary<int, Bullet> _bulletViews = new Dictionary<int, Bullet>();
    // 敌人实体ID到显示对象的映射
    private Dictionary<int, EnemyBase> _enemyViews = new Dictionary<int, EnemyBase>();
    // 玩家实体ID到显示对象的映射
    private Dictionary<int, PlayerController> _playerViews = new Dictionary<int, PlayerController>();

    public static event System.Action<int, int> OnPlayerHpChanged;
    public static event System.Action<int, int> OnPlayerSpawned;
    public static event System.Action<int> OnPlayerRespawnCountDownChanged;

    public SimulationRunMode RunMode => _runMode;
    public bool IsReplayRunning => _runMode == SimulationRunMode.Replay;

    // Start is called before the first frame update
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
        playerController.SetSimulationDriven(true);
        CacheInitialSceneState(playerController);
        SetLiveMode(playerController);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Update is called once per frame
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
        SyncPlayerView(oldWorld.Player, newWorld.Player);
        SyncBulletViews(newWorld.Bullets);
        SyncEnemyView(newWorld.Enemies);

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

    private void SyncBulletViews(BulletSimState[] bullets)
    {
        if (BulletManager.Instance == null)
        {
            return;
        }

        HashSet<int> activeBulletIds = new HashSet<int>();
        foreach (var bullet in bullets)
        {
            activeBulletIds.Add(bullet.EntityId);
            if (!_bulletViews.ContainsKey(bullet.EntityId))
            {
                Bullet bulletView = BulletManager.Instance.SpawnBullet(
                    bullet.Position.ToVector3(),
                    new Vector3((float)bullet.Direction.x, (float)bullet.Direction.y, (float)bullet.Direction.z),
                    (float)bullet.Speed,
                    (float)bullet.Damage,
                    bullet.RemainingLifetimeTicks * (1.0f / config.TickRate),
                    bullet.Faction
                );
                if (bulletView != null)
                {
                    _bulletViews[bullet.EntityId] = bulletView;
                }
            }
            else
            {
                Bullet bulletView = _bulletViews[bullet.EntityId];
                if (bulletView == null)
                {
                    _bulletViews.Remove(bullet.EntityId);
                    continue;
                }

                bulletView.transform.position = bullet.Position.ToVector3();
            }
        }

        int[] toRemove = _bulletViews.Keys.Where(id => !activeBulletIds.Contains(id)).ToArray();
        foreach (var id in toRemove)
        {
            Bullet bulletView = _bulletViews[id];
            if (bulletView != null)
            {
                BulletManager.Instance.RecycleBullet(bulletView);
            }

            _bulletViews.Remove(id);
        }
    }

    private void SyncEnemyView(EnemySimState[] enemies)
    {
        for (int i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            if (!_enemyViews.TryGetValue(enemy.EntityId, out EnemyBase enemyView))
            {
                continue;
            }
            if (enemyView == null)
            {
                continue;
            }
            if (!enemy.IsAlive)
            {
                if (enemyView.gameObject.activeSelf)
                {
                    enemyView.gameObject.SetActive(false);
                }
                continue;
            }
            enemyView.transform.position = enemy.Position.ToVector3();
        }
    }

    private void SyncPlayerView(PlayerSimState oldPlayerState, PlayerSimState newPlayerState)
    {
        PlayerController playerController;
        if (!_playerViews.TryGetValue(_playerID, out playerController))
        {
            Debug.LogError("SimulationDriver: PlayerController was not found.");
            return;
        }
        PlayerBase playerBase = playerController.GetComponent<PlayerBase>();
        if (!playerBase)
        {
            Debug.LogError("SimulationDriver: PlayerBase was not found.");
            return;
        }
        // 玩家死亡、复活事件触发
        if (!newPlayerState.IsAlive)
        {
            playerController.gameObject.SetActive(false);
            OnPlayerRespawnCountDownChanged?.Invoke(newPlayerState.RespawnCountdownTicks);
        }
        else
        {
            playerController.gameObject.SetActive(true);
            playerBase.UpdateInvincibleVisual(newPlayerState.IsInvincible);
            if (!oldPlayerState.IsAlive)
            {
                OnPlayerRespawnCountDownChanged?.Invoke(newPlayerState.RespawnCountdownTicks);
                OnPlayerSpawned?.Invoke((int)newPlayerState.Hp, (int)config.PlayerMaxHp);
            }
        }

        // HUD血量
        if (oldPlayerState.Hp != newPlayerState.Hp)
        {
            // 触发血量变化事件
            OnPlayerHpChanged?.Invoke((int)newPlayerState.Hp, (int)config.PlayerMaxHp);
        }

        FixVector3 simulatedPosition = newPlayerState.Position;
        playerController.transform.position = simulatedPosition.ToVector3();
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
        if (playerController == null)
        {
            _playerViews.TryGetValue(_playerID, out playerController);
        }

        if (playerController == null)
        {
            Debug.LogError("SimulationDriver: PlayerController was not found when switching to live mode.");
            _inputSource = null;
            return;
        }

        _inputSource = new LiveInputSource(playerController);
        _runMode = SimulationRunMode.Live;
        _verifier = null;
        _recorder = new ReplayRecorder();
        _recorder.BeginRecording(config, 0);
        ResetSimulationWorld(playerController);
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

        if (!_playerViews.TryGetValue(_playerID, out PlayerController playerController) || playerController == null)
        {
            Debug.LogError("SimulationDriver: PlayerController was not found when switching to replay mode.");
            return false;
        }

        ResetSimulationWorld(playerController);

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

    private void ResetSimulationWorld(PlayerController playerController)
    {
        _accumulator = 0f;
        _nextEnemyEntityID = 1;

        ClearBulletViews();
        RestoreSceneActors(playerController);

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

        _playerViews[_playerID] = playerController;
        var enemies = GetEnemySimStates();
        WorldSnapshot initialWorld = new WorldSnapshot(0, config, initialPlayer, new BulletSimState[0], enemies);
        _runner = new DeterministicSimulationRunner(initialWorld, config);
        OnPlayerSpawned?.Invoke((int)initialPlayer.Hp, (int)config.PlayerMaxHp);
    }

    // 载入初始场景对象
    private void RestoreSceneActors(PlayerController playerController)
    {
        playerController.gameObject.SetActive(true);
        playerController.transform.position = _initialPlayerPosition;

        for (int i = 0; i < _initialSceneEnemies.Count; i++)
        {
            EnemyBase enemy = _initialSceneEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            if (_initialEnemyPositions.TryGetValue(enemy, out Vector3 initialPosition))
            {
                enemy.gameObject.SetActive(true);
                enemy.transform.position = initialPosition;
            }
        }
    }

    private void ClearBulletViews()
    {
        if (BulletManager.Instance != null)
        {
            foreach (KeyValuePair<int, Bullet> pair in _bulletViews)
            {
                if (pair.Value != null)
                {
                    BulletManager.Instance.RecycleBullet(pair.Value);
                }
            }
        }

        _bulletViews.Clear();
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
        _enemyViews.Clear();
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
            EnemySimState enemySimState = new EnemySimState(enemyEntityId, fixPos, new FixVector3(Fix64.One, Fix64.Zero, Fix64.Zero), (Fix64)enemy.MaxHp, (Fix64)enemy.MaxHp, (Fix64)enemy.HitRadius, config.EnemyMoveSpeed, 0);
            enemySimStates.Add(enemySimState);
            _enemyViews[enemyEntityId] = enemy;
            _nextEnemyEntityID++;
        }
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
