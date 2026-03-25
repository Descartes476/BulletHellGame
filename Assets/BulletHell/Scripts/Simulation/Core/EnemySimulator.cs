namespace BulletHell.Simulation.Core
{
    public static class EnemySimulator
    {
        public static EnemySimState Step(in EnemySimState currentEnemy, in SimulationConfig config)
        {
            FixVector2 moveDirection2 = currentEnemy.MoveDirection.GetNormalizedOr(new FixVector2(Fix64.One, Fix64.Zero));
            FixVector3 moveDirection = new FixVector3(moveDirection2.x, moveDirection2.y, Fix64.Zero);
            FixVector3 newPostion = currentEnemy.Position + moveDirection * currentEnemy.Speed * config.TickDeltaTime;
            return new EnemySimState(
                currentEnemy.EntityId,
                newPostion,
                moveDirection2,
                currentEnemy.Hp,
                currentEnemy.MaxHp,
                currentEnemy.HitRadius,
                currentEnemy.Speed
            );
        }
    }
}
