using UnityEngine;

[CreateAssetMenu(fileName = "RandomAimPattern", menuName = "BulletHell/Shoot Pattern/Random Aim")]
public class RandomAimPattern : EnemyShootPattern
{
    public override void Shoot(EnemyShooter enemyShooter, Vector3 shootPos, Vector3 playerPos)
    {
        Vector3 toPlayer = playerPos - shootPos;
        Vector3 baseDir = new Vector3(toPlayer.x, toPlayer.y, 0f).normalized;
        float jitter = Random.Range(-enemyShooter.AimJitterDegrees, enemyShooter.AimJitterDegrees);
        Vector3 direction = Rotate(baseDir, jitter);
        enemyShooter.Shoot(direction);
    }
}
