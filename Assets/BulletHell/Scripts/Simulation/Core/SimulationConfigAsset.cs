using UnityEngine;

namespace BulletHell.Simulation.Core
{
    [CreateAssetMenu(fileName = "SimulationConfigAsset", menuName = "BulletHell/Simulation Config")]
    public sealed class SimulationConfigAsset : ScriptableObject
    {
        [Header("Tick")]
        [SerializeField] private int tickRate = 60;

        [Header("Player")]
        [SerializeField] private float playerMoveSpeed = 8f;
        [SerializeField] private int playerFireIntervalTicks = 18;
        [SerializeField] private float playerBulletSpeed = 10f;
        [SerializeField] private float playerBulletDamage = 1f;
        [SerializeField] private int playerBulletLifetimeTicks = 180;
        [SerializeField] private float playerBulletHitRadius = 0.1f;
        [SerializeField] private float playerHitRadius = 0.5f;
        [SerializeField] private float playerMaxHp = 10f;
        [SerializeField] private int playerRespawnTicks = 180;
        [SerializeField] private int playerInvincibleTicks = 180;

        [Header("Play Area")]
        [SerializeField] private Vector2 playAreaMin = new Vector2(-8f, -4.5f);
        [SerializeField] private Vector2 playAreaMax = new Vector2(8f, 4.5f);

        [Header("Enemy")]
        [SerializeField] private int enemyFireIntervalTicks = 54;
        [SerializeField] private float enemyBulletSpeed = 5f;
        [SerializeField] private float enemyBulletDamage = 1f;
        [SerializeField] private int enemyBulletLifetimeTicks = 180;
        [SerializeField] private float enemyBulletHitRadius = 0.1f;
        [SerializeField] private float enemyMoveSpeed = 0.5f;
        

        public SimulationConfig ToSimulationConfig()
        {
            return new SimulationConfig(
                tickRate,
                (Fix64)playerMoveSpeed,
                playerFireIntervalTicks,
                (Fix64)playerBulletSpeed,
                (Fix64)playerBulletDamage,
                playerBulletLifetimeTicks,
                (Fix64)playerBulletHitRadius,
                (Fix64)playerHitRadius,
                (Fix64)playerMaxHp,
                playerRespawnTicks,
                playerInvincibleTicks,
                new FixVector2((Fix64)playAreaMin.x, (Fix64)playAreaMin.y),
                new FixVector2((Fix64)playAreaMax.x, (Fix64)playAreaMax.y),
                enemyFireIntervalTicks,
                (Fix64)enemyBulletSpeed,
                (Fix64)enemyBulletDamage,
                enemyBulletLifetimeTicks,
                (Fix64)enemyBulletHitRadius,
                (Fix64)enemyMoveSpeed
                );
        }
    }
}
