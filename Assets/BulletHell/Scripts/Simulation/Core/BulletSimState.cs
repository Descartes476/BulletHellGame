namespace BulletHell.Simulation.Core
{
    public readonly struct BulletSimState
    {
        public int EntityId { get; }
        public FixVector3 Position { get; }
        public FixVector3 Direction { get; }
        public Fix64 Speed { get; }
        public Fix64 Damage { get; }
        public Fix64 Radius { get; } // 子弹碰撞范围
        public int RemainingLifetimeTicks { get; }
        public BulletFaction Faction { get; }

        public bool IsAlive => RemainingLifetimeTicks > 0;

        public BulletSimState(
            int entityId,
            FixVector3 position,
            FixVector3 direction,
            Fix64 speed,
            Fix64 damage,
            Fix64 radius,
            int remainingLifetimeTicks,
            BulletFaction faction)
        {
            EntityId = entityId;
            Position = position;
            Direction = direction;
            Speed = speed;
            Damage = damage;
            Radius = radius;
            RemainingLifetimeTicks = remainingLifetimeTicks;
            Faction = faction;
        }
    }
}
