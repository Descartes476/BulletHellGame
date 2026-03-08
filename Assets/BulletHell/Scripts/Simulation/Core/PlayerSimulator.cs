namespace BulletHell.Simulation.Core
{
    public static class PlayerSimulator
    {
        public static PlayerSimState Step(in PlayerSimState currentState, in InputFrame inputFrame, in SimulationConfig config)
        {
            // 瞄准方向
            Fix64 aimX = (Fix64)inputFrame.AimX/1000;
            Fix64 aimY = (Fix64)inputFrame.AimY/1000;
            FixVector2 aimDirection = new FixVector2(aimX, aimY);
            if(aimX == Fix64.Zero && aimY == Fix64.Zero)
            {
                aimDirection = currentState.AimDirection;
            }
            FixVector2 moveInput = new FixVector2(inputFrame.MoveX, inputFrame.MoveY);
            if(moveInput != FixVector2.Zero)
            {
                moveInput.Normalize();
            }

            // 位置变化
            FixVector2 deltaMove = moveInput * config.PlayerMoveSpeed * config.TickDeltaTime;
            FixVector2 nextPosition = currentState.Position + deltaMove;
            
            //限制玩家位置
            Fix64 clampedX = nextPosition.x;
            if (clampedX < config.PlayAreaMin.x) clampedX = config.PlayAreaMin.x;
            if (clampedX > config.PlayAreaMax.x) clampedX = config.PlayAreaMax.x;            
            Fix64 clampedY = nextPosition.y;
            if (clampedY < config.PlayAreaMin.y) clampedY = config.PlayAreaMin.y;
            if (clampedY > config.PlayAreaMax.y) clampedY = config.PlayAreaMax.y;
            nextPosition = new FixVector2(clampedX, clampedY);

            int nextCooldown = currentState.FireCooldownTicks > 0
                ? currentState.FireCooldownTicks - 1
                : 0;

            

            return new PlayerSimState(
                currentState.EntityId,
                nextPosition,
                aimDirection,
                currentState.Hp,
                currentState.IsAlive,
                nextCooldown,
                currentState.RespawnCountdownTicks,
                currentState.InvincibleTicks
            );
        }
    }

}