using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 1f;
    public float maxLifetime = 5f; // 子弹存在时间上限
    private float lifetime = 0f; // 子弹已存在时间
    private Vector2 moveDir;

    public void Init(Vector3 position, Vector2 direction, float speed = 10f, float damage = 1f, float lifetime = 5f)
    {
        if (direction.sqrMagnitude < 1e-8f)
        {
            moveDir = Vector2.zero;
        }
        else
        {
            moveDir = direction.normalized;
        }
        transform.position = position;
        this.lifetime = 0f;
        this.maxLifetime = lifetime;
        this.damage = damage;
        this.speed = speed;
    }

    public bool Tick(float dt)
    {
        lifetime += dt;
        // 是否超过最大存在时间
        if (lifetime >= maxLifetime)
        {
            return false;
        }

        Vector3 pos = transform.position;
        pos.x += moveDir.x * speed * dt;
        pos.y += moveDir.y * speed * dt;
        transform.position = pos;
        return true;
    }

}
