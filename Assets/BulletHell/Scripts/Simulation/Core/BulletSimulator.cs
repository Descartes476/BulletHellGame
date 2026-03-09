namespace BulletHell.Simulation.Core
{
    public static class BulletSimulator
    {
        public static BulletSimState Step(in BulletSimState currentBullet, in SimulationConfig config)
        {
            FixVector3 deltaMove = new FixVector3(currentBullet.Direction.x, currentBullet.Direction.y, Fix64.Zero) * currentBullet.Speed * config.TickDeltaTime;
            FixVector3 newPosition = currentBullet.Position + deltaMove;
            int nextLifetimeTick = currentBullet.RemainingLifetimeTicks > 0 ? currentBullet.RemainingLifetimeTicks-1 : 0;
            return new BulletSimState(
                currentBullet.EntityId,
                newPosition,
                currentBullet.Direction,
                currentBullet.Speed,
                currentBullet.Damage,
                currentBullet.Radius,
                nextLifetimeTick,
                currentBullet.Faction
            );
        }
    }
}