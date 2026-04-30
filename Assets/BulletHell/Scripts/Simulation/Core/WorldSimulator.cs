using System.Collections.Generic;

namespace BulletHell.Simulation.Core
{
    public static class WorldSimulator
    {
        private static readonly FixVector3[] EnemyMoveDirections = new FixVector3[]
        {
            new FixVector3(Fix64.One, Fix64.Zero, Fix64.Zero),
            new FixVector3(-Fix64.One, Fix64.Zero, Fix64.Zero),
            new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero),
            new FixVector3(Fix64.Zero, -Fix64.One, Fix64.Zero),
            new FixVector3(Fix64.One, Fix64.One, Fix64.Zero),
            new FixVector3(Fix64.One, -Fix64.One, Fix64.Zero),
            new FixVector3(-Fix64.One, Fix64.One, Fix64.Zero),
            new FixVector3(-Fix64.One, -Fix64.One, Fix64.Zero)
        };

        public static WorldSnapshot Step(in WorldSnapshot currentWorld, in InputFrame playerInput, DeterministicRandom random)
        {
            SimulationConfig config = currentWorld.Config;
            //玩家推进
            PlayerSimState nextPlayerState = PlayerSimulator.Step(currentWorld.Player, playerInput, config);
            int nextTick = currentWorld.Tick + 1;

            //子弹推进
            List<BulletSimState> nextBullets = new List<BulletSimState>(currentWorld.Bullets.Length);
            for (int i = 0; i < currentWorld.Bullets.Length; i++)
            {
                BulletSimState currentBullet = currentWorld.Bullets[i];
                if (!currentBullet.IsAlive)
                {
                    continue;
                }

                BulletSimState nextBullet = BulletSimulator.Step(in currentBullet, config);
                if (!nextBullet.IsAlive)
                {
                    continue;
                }

                nextBullets.Add(nextBullet);
            }

            //敌人推进
            EnemySimState[] enemies = currentWorld.Enemies;
            List<EnemySimState> nextEnemySimStates = new List<EnemySimState>();
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemySimState currentEnemy = enemies[i];
                if (!currentEnemy.IsAlive)
                {
                    nextEnemySimStates.Add(currentEnemy);
                    continue;
                }

                FixVector3 moveDirection = currentEnemy.MoveDirection;
                if (currentEnemy.MoveDecisionCooldownTicks <= 0)
                {
                    moveDirection = EnemyMoveDirections[random.RangeInt(0, EnemyMoveDirections.Length)];
                }

                EnemySimState nextEnemy = EnemySimulator.Step(in currentEnemy, config, moveDirection);
                nextEnemySimStates.Add(nextEnemy);
            }

            return new WorldSnapshot(nextTick, config, nextPlayerState, nextBullets.ToArray(), nextEnemySimStates.ToArray());
        }
    }
}
