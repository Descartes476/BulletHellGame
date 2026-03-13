using System.Collections.Generic;

namespace BulletHell.Simulation.Core
{
    public static class WorldSimulator
    {
        public static WorldSnapshot Step(in WorldSnapshot currentWorld, in InputFrame playerInput)
        {
            PlayerSimState nextPlayerState = PlayerSimulator.Step(currentWorld.Player, playerInput, currentWorld.Config);
            int nextTick = currentWorld.Tick + 1;
            List<BulletSimState> nextBullets = new List<BulletSimState>(currentWorld.Bullets.Length);
            for (int i = 0; i < currentWorld.Bullets.Length; i++)
            {
                BulletSimState currentBullet = currentWorld.Bullets[i];
                if (!currentBullet.IsAlive)
                {
                    continue;
                }

                BulletSimState nextBullet = BulletSimulator.Step(in currentBullet, currentWorld.Config);
                if (!nextBullet.IsAlive)
                {
                    continue;
                }

                nextBullets.Add(nextBullet);
            }
            return new WorldSnapshot(nextTick, currentWorld.Config, nextPlayerState, nextBullets.ToArray(), currentWorld.Enemies);
        }
    }
}

