using UnityEngine;

// 负责维护本局的全局状态，并通过事件把分数、复活倒计时等变化广播给 UI 与其他系统。
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float respawnDelay = 3f;

    // 当前局内累计分数，由敌人死亡事件驱动增长。
    public int Score { get; private set; }
    // 预留的全局结束状态；当前流程里主要使用复活阶段而非真正 Game Over。
    public bool IsGameOver { get; private set; }
    // 是否正处于玩家死亡后的延迟复活阶段。
    public bool IsRespawning => _isRespawning;
    // 当前剩余的复活倒计时，供 HUD 等系统读取展示。
    public float RespawnCountdown => _respawnCountdown;

    public static event System.Action<int> OnScoreChanged;
    public static event System.Action OnGameOver;
    public static event System.Action<float> OnRespawnCountdownChanged;
    public static event System.Action<PlayerBase> OnPlayerRespawned;

    // 标记 Update 是否需要继续推进复活倒计时。
    private bool _isRespawning;
    // 玩家再次启用前剩余的等待时间。
    private float _respawnCountdown;
    // 记录本次等待复活的玩家对象，倒计时结束后对它调用 Respawn。
    private PlayerBase _pendingRespawnPlayer;

    // 这里只处理敌人死亡后的分数结算，不直接参与敌人销毁逻辑。
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Score = 0;
        IsGameOver = false;
        _isRespawning = false;
        _respawnCountdown = 0f;
        _pendingRespawnPlayer = null;
    }

    private void HandleEnemyDied(EnemyBase enemy)
    {
        Score += 10;
        OnScoreChanged?.Invoke(Score);
    }

    private void HandlePlayerDied(PlayerBase player)
    {
        if (IsGameOver)
            return;

        // 玩家死亡后先进入延迟复活阶段，真正复活由 Update 中的倒计时归零触发。
        _pendingRespawnPlayer = player;
        _respawnCountdown = respawnDelay;
        _isRespawning = true;
        OnRespawnCountdownChanged?.Invoke(_respawnCountdown);
    }

    void Update()
    {
        if (!_isRespawning)
            return;

        _respawnCountdown -= Time.deltaTime;
        if (_respawnCountdown > 0f)
        {
            OnRespawnCountdownChanged?.Invoke(_respawnCountdown);
            return;
        }

        _isRespawning = false;
        _respawnCountdown = 0f;
        OnRespawnCountdownChanged?.Invoke(0f);

        if (_pendingRespawnPlayer != null)
        {
            // 先恢复玩家实体，再广播复活事件，确保监听方拿到的是已可用对象。
            _pendingRespawnPlayer.Respawn();
            OnPlayerRespawned?.Invoke(_pendingRespawnPlayer);
            _pendingRespawnPlayer = null;
        }
    }

    void OnEnable()
    {
        EnemyBase.OnEnemyDied += HandleEnemyDied;
        PlayerBase.OnPlayerDied += HandlePlayerDied;
    }

    void OnDisable()
    {
        EnemyBase.OnEnemyDied -= HandleEnemyDied;
        PlayerBase.OnPlayerDied -= HandlePlayerDied;
    }
}
