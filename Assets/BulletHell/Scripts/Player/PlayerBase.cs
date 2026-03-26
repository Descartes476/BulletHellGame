using System.Collections.Generic;
using UnityEngine;

// 管理玩家的生命值、出生点、死亡/复活，以及复活后的短暂无敌表现。
public class PlayerBase : MonoBehaviour
{

    // 玩家最大生命值；重新启用或复活时会据此重置当前血量。
    [SerializeField] private float hp = 100f;
    // 用于子弹命中检测的近似圆形半径。
    [SerializeField] private float hitRadius = 0.5f;
    // 复活后持续多久不受伤害。
    [SerializeField] private float respawnInvincibleDuration = 2f;
    // 无敌闪烁时允许的最低透明度。
    [SerializeField] private float invincibleBlinkMinAlpha = 0.35f;
    // 无敌闪烁的频率控制值。
    [SerializeField] private float invincibleBlinkSpeed = 10f;
    // 当前实际生命值。
    private float _currentHp;
    // 记录出生/复活位置。
    private Vector3 _spawnPosition;
    // 标记是否已经缓存过出生点，避免重复覆盖。
    private bool _hasSpawnPosition;
    // 剩余无敌时间；大于 0 时玩家不会受伤。
    private float _invincibleTimer;
    // 缓存所有需要做闪烁表现的精灵渲染器。
    private SpriteRenderer[] _spriteRenderers;
    // 缓存原始颜色，用于在闪烁时只修改透明度并可恢复。
    private Color[] _originalColors;
    private static readonly List<PlayerBase> activePlayersInternal = new List<PlayerBase>();   // 玩家注册表
    public static IReadOnlyList<PlayerBase> ActivePlayers => activePlayersInternal;
    public float HitRadius => hitRadius;
    public float CurrentHp => _currentHp;
    public float MaxHp => hp;
    public Vector3 SpawnPosition => _spawnPosition;
    public bool IsInvincible => _invincibleTimer > 0f;

    // 玩家事件
    public static event System.Action<PlayerBase> OnPlayerDied;
    public static event System.Action<PlayerBase> OnPlayerSpawned;
    public static event System.Action<PlayerBase> OnPlayerHpChanged;


    private void Update()
    {
        
    }


    private void Awake()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        _originalColors = new Color[_spriteRenderers.Length];
        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            _originalColors[i] = _spriteRenderers[i].color;
        }


    }


    // 通过透明度闪烁反馈无敌状态，不修改对象开关，避免影响碰撞和事件订阅。
    private void UpdateInvincibleVisual()
    {
        if (_spriteRenderers == null || _originalColors == null)
            return;

        float alpha = 1f;
        if (IsInvincible)
        {
            float t = Mathf.PingPong(Time.time * invincibleBlinkSpeed, 1f); // PingPong返回0到1再回到0
            alpha = Mathf.Lerp(invincibleBlinkMinAlpha, 1f, t);  // 从最小alpha渐变到完全可见
        }
        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            var renderer = _spriteRenderers[i];
            if (renderer == null)
                continue;

            Color color = _originalColors[i];
            color.a = alpha;
            renderer.color = color;
        }
    }

}
