namespace BulletHell.Simulation.Core
{
    public readonly struct EnemySimState
    {
        public int EntityId { get; } // 敌人唯一Id
        public FixVector3 Position { get; } // 敌人位置
        public FixVector3 MoveDirection { get; } // 敌人移动方向
        public Fix64 Hp { get; } // 敌人当前血量
        public Fix64 MaxHp { get; } // 敌人血量上限
        public Fix64 HitRadius { get; } // 受击范围
        public Fix64 Speed { get; } // 敌人速度
        public int FireCooldownTicks { get; } // 射击冷却
        public bool IsAlive => Hp > Fix64.Zero;
        public bool CanFire => FireCooldownTicks <= 0;

        public EnemySimState(int entityId, FixVector3 position, FixVector3 moveDir, Fix64 hp, Fix64 maxHp, Fix64 hitRadius, Fix64 speed, int fireCooldownTicks)
        {
            EntityId = entityId;
            Position = position;
            MoveDirection = moveDir;
            Hp = hp;
            MaxHp = maxHp;
            HitRadius = hitRadius;
            Speed = speed;
            FireCooldownTicks = fireCooldownTicks;
        }
    }
}
