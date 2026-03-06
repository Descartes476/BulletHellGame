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
        PlayerBase.OnPlayerSpawned += HandlePlayerSpawned;
        PlayerBase.OnPlayerHpChanged += HandlePlayerHpChanged;
        GameManager.OnScoreChanged += HandleScoreChanged;
        GameManager.OnGameOver += HandleGameOver;
        GameManager.OnRespawnCountdownChanged += HandleRespawnCountdownChanged;
        GameManager.OnPlayerRespawned += HandlePlayerRespawned;

        RefreshInitialView();
    }

    void OnDisable()
    {
        PlayerBase.OnPlayerSpawned -= HandlePlayerSpawned;
        PlayerBase.OnPlayerHpChanged -= HandlePlayerHpChanged;
        GameManager.OnScoreChanged -= HandleScoreChanged;
        GameManager.OnGameOver -= HandleGameOver;
        GameManager.OnRespawnCountdownChanged -= HandleRespawnCountdownChanged;
        GameManager.OnPlayerRespawned -= HandlePlayerRespawned;
    }

    // 兼容场景中 HUD 晚于 GameManager / Player 创建的情况，启用时主动同步一次当前状态。
    private void RefreshInitialView()
    {
        HandleScoreChanged(GameManager.Instance != null ? GameManager.Instance.Score : 0);

        var players = PlayerBase.ActivePlayers;
        if (players != null && players.Count > 0)
            SetHpText(players[0]);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(GameManager.Instance != null && GameManager.Instance.IsGameOver);

        if (respawnTxt != null)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsRespawning)
                respawnTxt.text = $"Revive In: {Mathf.CeilToInt(GameManager.Instance.RespawnCountdown)}";
            else
                respawnTxt.text = string.Empty;
        }
    }

    private void HandlePlayerSpawned(PlayerBase player)
    {
        SetHpText(player);
    }

    private void HandlePlayerHpChanged(PlayerBase player)
    {
        SetHpText(player);
    }

    private void HandleScoreChanged(int score)
    {
        if (scoreTxt != null)
            scoreTxt.text = $"Score: {score}";
    }

    private void HandleGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    private void HandleRespawnCountdownChanged(float countdown)
    {
        // 这里把复活倒计时阶段也视为一种临时 Game Over 画面，用于遮罩和倒计时提示。
        if (gameOverPanel != null)
            gameOverPanel.SetActive(countdown > 0f);

        if (respawnTxt == null)
            return;

        if (countdown > 0f)
            respawnTxt.text = $"Revive In: {Mathf.CeilToInt(countdown)}";
        else
            respawnTxt.text = string.Empty;
    }

    private void HandlePlayerRespawned(PlayerBase player)
    {
        SetHpText(player);
        if (respawnTxt != null)
            respawnTxt.text = string.Empty;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void SetHpText(PlayerBase player)
    {
        if (hpTxt == null || player == null)
            return;

        int hp = Mathf.Max(0, Mathf.CeilToInt(player.CurrentHp));
        int maxHp = Mathf.Max(1, Mathf.CeilToInt(player.MaxHp));
        hpTxt.text = $"HP: {hp}/{maxHp}";
    }
}
