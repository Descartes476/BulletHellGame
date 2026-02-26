using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 1f;
    public float fMaxLifetime = 5f; // 子弹存在时间上限
    private float fLifetime = 0f; // 子弹已存在时间
    private Vector2 vMoveDir;

    public void Init(Vector2 vDirection)
    {
        if (vDirection.sqrMagnitude < 1e-8f)
        {
            vMoveDir = Vector2.zero;
        }
        else
        {
            vMoveDir = vDirection.normalized;
        }
        fLifetime = 0f;
    }

    public bool Tick(float dt)
    {
        fLifetime += dt;
        // 是否超过最大存在时间
        if (fLifetime >= fMaxLifetime)
        {
            return false;
        }

        Vector3 pos = transform.position;
        pos.x += vMoveDir.x * speed * dt;
        pos.y += vMoveDir.y * speed * dt;
        transform.position = pos;
        return true;
    }

}
