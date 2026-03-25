using UnityEngine;

[CreateAssetMenu(fileName = "FanPattern", menuName = "BulletHell/Shoot Pattern/Fan")]
public class FanShootPattern : EnemyShootPattern
{
    public int bulletCount = 5; // 一次性发射的子弹数量
    public float spreadDegrees = 60f; //扇形角度

    public override void Shoot(EnemyShooter enemyShooter, Vector3 shootPos, Vector3 playerPos)
    {
        if (bulletCount <= 0)
            return;

        Vector3 toPlayer = playerPos - shootPos;
        Vector3 baseDir = new Vector3(toPlayer.x, toPlayer.y, 0f).normalized;
        if (baseDir.sqrMagnitude <= 0.0001f)
            baseDir = Vector3.up;

        if (bulletCount == 1)
        {
            enemyShooter.Shoot(baseDir);
            return;
        }

        float spread = spreadDegrees;
        if (spread < 0f)
            spread = 0f;

        float bulletGapAngle = spread / (bulletCount - 1);
        float baseAngle = -spread * 0.5f;
        for (int i = 0; i < bulletCount; i++)
        {
            Vector3 direction = Rotate(baseDir, baseAngle + bulletGapAngle * i);
            enemyShooter.Shoot(direction);
        }
    }
}
