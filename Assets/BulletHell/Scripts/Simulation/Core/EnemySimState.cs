namespace BulletHell.Simulation.Core
{
    public readonly struct EnemySimState
    {
        public int EntityId { get; }
        public FixVector3 Position { get; }
        public Fix64 Hp { get; }
        public Fix64 MaxHp { get; }
        public Fix64 HitRadius { get; }
        public bool IsAlive => Hp > 0;

        public EnemySimState(int entityId, FixVector3 position, Fix64 hp, Fix64 maxHp, Fix64 hitRadius)
        {
            
            EntityId = entityId;
            Position = position;
            Hp = hp;
            MaxHp = maxHp;
            HitRadius = hitRadius;
        }
    }
}
