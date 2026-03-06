using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TMP_Text hpTxt;
    [SerializeField] private TMP_Text scoreTxt;
    [SerializeField] private GameObject gameOverPanel;

    void OnEnable()
    {
        PlayerBase.OnPlayerSpawned += HandlePlayerSpawned;
        PlayerBase.OnPlayerHpChanged += HandlePlayerHpChanged;
        GameManager.OnScoreChanged += HandleScoreChanged;
        GameManager.OnGameOver += HandleGameOver;

        RefreshInitialView();
    }

    void OnDisable()
    {
        PlayerBase.OnPlayerSpawned -= HandlePlayerSpawned;
        PlayerBase.OnPlayerHpChanged -= HandlePlayerHpChanged;
        GameManager.OnScoreChanged -= HandleScoreChanged;
        GameManager.OnGameOver -= HandleGameOver;
    }

    private void RefreshInitialView()
    {
        HandleScoreChanged(GameManager.Instance != null ? GameManager.Instance.Score : 0);

        var players = PlayerBase.ActivePlayers;
        if (players != null && players.Count > 0)
            SetHpText(players[0]);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(GameManager.Instance != null && GameManager.Instance.IsGameOver);
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

    private void SetHpText(PlayerBase player)
    {
        if (hpTxt == null || player == null)
            return;

        int hp = Mathf.Max(0, Mathf.CeilToInt(player.CurrentHp));
        int maxHp = Mathf.Max(1, Mathf.CeilToInt(player.MaxHp));
        hpTxt.text = $"HP: {hp}/{maxHp}";
    }
}
