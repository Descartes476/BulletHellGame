namespace BulletHell.Simulation.Core
{
    public readonly struct EnemySimState
    {
        public int EntityId { get; }
        public FixVector3 Position { get; }
        public FixVector3 MoveDirection { get; }
        public Fix64 Hp { get; }
        public Fix64 MaxHp { get; }
        public Fix64 HitRadius { get; }
        public Fix64 Speed { get; }
        public bool IsAlive => Hp > 0;

        public EnemySimState(int entityId, FixVector3 position, FixVector3 moveDir, Fix64 hp, Fix64 maxHp, Fix64 hitRadius, Fix64 speed)
        {
            EntityId = entityId;
            Position = position;
            MoveDirection = moveDir;
            Hp = hp;
            MaxHp = maxHp;
            HitRadius = hitRadius;
            Speed = speed;
        }
    }
}
