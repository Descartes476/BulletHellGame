namespace BulletHell.Simulation.Core
{
    public static class EnemySimulator
    {
        public static EnemySimState Step(in EnemySimState currentEnemy, in SimulationConfig config)
        {
            // 敌人移动
            FixVector3 moveDirection = currentEnemy.MoveDirection.GetNormalizedOr(new FixVector3(Fix64.One, Fix64.Zero, Fix64.Zero));
            FixVector3 newPostion = currentEnemy.Position + moveDirection * currentEnemy.Speed * config.TickDeltaTime;

            int fireCooldownTicks = currentEnemy.FireCooldownTicks; 
            fireCooldownTicks = fireCooldownTicks>0 ? fireCooldownTicks-1 : 0;
            return new EnemySimState(
                currentEnemy.EntityId,
                newPostion,
                moveDirection,
                currentEnemy.Hp,
                currentEnemy.MaxHp,
                currentEnemy.HitRadius,
                currentEnemy.Speed,
                fireCooldownTicks
            );
        }
    }
}
