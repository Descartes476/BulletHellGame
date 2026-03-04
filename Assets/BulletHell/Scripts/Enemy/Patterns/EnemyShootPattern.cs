using UnityEngine;

public abstract class EnemyShootPattern : ScriptableObject
{
    public float weight = 1f;  //随机权重
    public abstract void Shoot(EnemyShooter enemyShooter, Vector3 shootPos, Vector3 playerPos);

    protected static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
}
