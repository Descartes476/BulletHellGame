using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Score { get; private set; }
    public bool IsGameOver { get; private set; }

    public static event System.Action<int> OnScoreChanged;
    public static event System.Action OnGameOver;

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

        IsGameOver = true;
        OnGameOver?.Invoke();
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
