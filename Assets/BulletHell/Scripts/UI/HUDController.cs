using TMPro;
using UnityEngine;

// 只负责把游戏事件映射到 HUD 展示，不保存游戏状态本身。
public class HUDController : MonoBehaviour
{
    // 玩家血量文本。
    [SerializeField] private TMP_Text hpTxt;
    // 当前分数文本。
    [SerializeField] private TMP_Text scoreTxt;
    // 复活倒计时文本。
    [SerializeField] private TMP_Text respawnTxt;
    // 复活等待或结束态时使用的遮罩/面板。
    [SerializeField] private GameObject gameOverPanel;

    void OnEnable()
    {
        SimulationDriver.OnPlayerSpawned += HandlePlayerSpawned;
        SimulationDriver.OnPlayerHpChanged += HandlePlayerHpChanged;
        SimulationDriver.OnPlayerRespawnCountDownChanged += HandleRespawnCountdownChanged;
        GameManager.OnScoreChanged += HandleScoreChanged;

        RefreshInitialView();
    }

    void OnDisable()
    {
        SimulationDriver.OnPlayerSpawned -= HandlePlayerSpawned;
        SimulationDriver.OnPlayerHpChanged -= HandlePlayerHpChanged;
        SimulationDriver.OnPlayerRespawnCountDownChanged -= HandleRespawnCountdownChanged;
        GameManager.OnScoreChanged -= HandleScoreChanged;
    }

    // 启用时主动同步一次当前状态。
    private void RefreshInitialView()
    {
        HandleScoreChanged(GameManager.Instance != null ? GameManager.Instance.Score : 0);

        if (SimulationDriver.Instance != null && SimulationDriver.Instance.TryGetPlayerHudState(out int currentHp, out int maxHp, out int respawnCountdownTicks))
        {
            SetHpText(currentHp, maxHp);
            HandleRespawnCountdownChanged(respawnCountdownTicks);
        }
        else
        {
            HandleRespawnCountdownChanged(0);
        }
    }

    private void HandlePlayerSpawned(int currentHp, int maxHp)
    {
        SetHpText(currentHp, maxHp);
    }

    private void HandlePlayerHpChanged(int currentHp, int maxHp)
    {
        SetHpText(currentHp, maxHp);
    }

    private void HandleScoreChanged(int score)
    {
        if (scoreTxt != null)
            scoreTxt.text = $"Score: {score}";
    }


    private void HandleRespawnCountdownChanged(int countdownTick)
    {
        // 这里把复活倒计时阶段也视为一种临时 Game Over 画面，用于遮罩和倒计时提示。
        if (gameOverPanel != null)
            gameOverPanel.SetActive(countdownTick > 0f);

        if (respawnTxt == null)
            return;

        if (countdownTick > 0f)
            respawnTxt.text = $"Revive In: {Mathf.CeilToInt(countdownTick)}";
        else
            respawnTxt.text = string.Empty;
    }

    private void SetHpText(int currentHp, int maxHp)
    {
        currentHp = Mathf.Max(0, Mathf.CeilToInt(currentHp));
        maxHp = Mathf.Max(1, Mathf.CeilToInt(maxHp));
        hpTxt.text = $"HP: {currentHp}/{maxHp}";
    }

}
