using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TMP_Text hpTxt;
    [SerializeField] private TMP_Text scoreTxt;
    [SerializeField] private TMP_Text respawnTxt;
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
