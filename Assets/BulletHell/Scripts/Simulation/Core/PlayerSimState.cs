namespace BulletHell.Simulation.Core
{
    public struct PlayerSimState
    {
        public int EntityId { get; private set; }  // 玩家ID
        public FixVector2 Position { get; private set; }  // 玩家位置
        public FixVector2 AimDirection { get; private set; }  // 玩家当前瞄准方向
        public Fix64 Hp { get; private set; }
        public bool IsAlive { get; private set; }
        public int FireCooldownTicks { get; private set; }  // 射击冷却（单位：tick）
        public int RespawnCountdownTicks { get; private set; }  // 复活倒计时（单位：tick）
        public int InvincibleTicks { get; private set; }  // 无敌倒计时（单位：tick）
        public bool CanFire => IsAlive && FireCooldownTicks <= 0;
        public bool IsRespawning => !IsAlive && RespawnCountdownTicks > 0;
        public bool IsInvincible => InvincibleTicks > 0;

        public PlayerSimState(int entityId, FixVector2 position, FixVector2 aimDirection, Fix64 hp, bool isAlive, int fireCooldownTicks, int respawnCountdownTicks, int invincibleTicks)
        {
            EntityId = entityId;
            Position = position;
            AimDirection = aimDirection;
            Hp = hp;
            IsAlive = isAlive;
            FireCooldownTicks = fireCooldownTicks;
            RespawnCountdownTicks = respawnCountdownTicks;
            InvincibleTicks = invincibleTicks;
        }

    }
}
