using UnityEngine;

[CreateAssetMenu(fileName = "RandomAimPattern", menuName = "BulletHell/Shoot Pattern/Random Aim")]
public class RandomAimPattern : EnemyShootPattern
{
    public override void Shoot(EnemyShooter enemyShooter, Vector3 shootPos, Vector3 playerPos)
    {
        Vector3 toPlayer = playerPos - shootPos;
        Vector2 baseDir = new Vector2(toPlayer.x, toPlayer.y).normalized;
        float jitter = Random.Range(-enemyShooter.AimJitterDegrees, enemyShooter.AimJitterDegrees);
        Vector2 direction = Rotate(baseDir, jitter);
        enemyShooter.Shoot(direction);
    }
}
