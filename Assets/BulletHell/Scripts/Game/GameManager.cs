using UnityEngine;

// 负责维护本局的全局状态，并通过事件把分数、复活倒计时等变化广播给 UI 与其他系统。
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 当前局内累计分数，由敌人死亡事件驱动增长。
    public int Score { get; private set; }
    // 预留的全局结束状态；当前流程里主要使用复活阶段而非真正 Game Over。
    public bool IsGameOver { get; private set; }

    public static event System.Action<int> OnScoreChanged;
    public static event System.Action OnGameOver;
    public static event System.Action<int> OnEnemyDied;

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
    }

    private void HandleEnemyDied(int enemyId)
    {
        Score += 10;
        OnScoreChanged?.Invoke(Score);
    }



    void OnEnable()
    {
        OnEnemyDied += HandleEnemyDied;
    }

    void OnDisable()
    {
        OnEnemyDied -= HandleEnemyDied;
    }

    public void TriggerEnemyDied(int entityId)
    {
        OnEnemyDied?.Invoke(entityId);
    }

}
