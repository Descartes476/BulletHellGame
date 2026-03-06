using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float respawnDelay = 3f;

    public int Score { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsRespawning => _isRespawning;
    public float RespawnCountdown => _respawnCountdown;

    public static event System.Action<int> OnScoreChanged;
    public static event System.Action OnGameOver;
    public static event System.Action<float> OnRespawnCountdownChanged;
    public static event System.Action<PlayerBase> OnPlayerRespawned;

    private bool _isRespawning;
    private float _respawnCountdown;
    private PlayerBase _pendingRespawnPlayer;

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
