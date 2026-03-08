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
        [SerializeField] private float playerBulletSpeed = 5f;
        [SerializeField] private float playerBulletDamage = 1f;
        [SerializeField] private int playerBulletLifetimeTicks = 180;

        [Header("Play Area")]
        [SerializeField] private Vector2 playAreaMin = new Vector2(-8f, -4.5f);
        [SerializeField] private Vector2 playAreaMax = new Vector2(8f, 4.5f);

        public SimulationConfig ToSimulationConfig()
        {
            return new SimulationConfig(
                tickRate,
                (Fix64)playerMoveSpeed,
                playerFireIntervalTicks,
                (Fix64)playerBulletSpeed,
                (Fix64)playerBulletDamage,
                playerBulletLifetimeTicks,
                new FixVector2((Fix64)playAreaMin.x, (Fix64)playAreaMin.y),
                new FixVector2((Fix64)playAreaMax.x, (Fix64)playAreaMax.y));
        }
    }
}
