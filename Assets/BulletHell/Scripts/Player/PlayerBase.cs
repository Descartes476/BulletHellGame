using System.Collections.Generic;
using UnityEngine;

// 管理玩家的生命值、出生点、死亡/复活，以及复活后的短暂无敌表现。
public class PlayerBase : MonoBehaviour
{
    // 无敌闪烁时允许的最低透明度。
    [SerializeField] private float invincibleBlinkMinAlpha = 0.35f;
    // 无敌闪烁的频率控制值。
    [SerializeField] private float invincibleBlinkSpeed = 10f;
    private SpriteRenderer[] _spriteRenderers;
    // 缓存原始颜色，用于在闪烁时只修改透明度并可恢复。
    private Color[] _originalColors;

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
    public void UpdateInvincibleVisual(bool isInvincible)
    {
        if (_spriteRenderers == null || _originalColors == null)
            return;

        float alpha = 1f;
        if (isInvincible)
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
