namespace BulletHell.Simulation.Core

{

    public static class PlayerSimulator

    {
        public static bool ShouldFire(PlayerSimState currentPlayerState, InputFrame inputFrame)
        {
            bool IsAlive = currentPlayerState.IsAlive;
            if(!IsAlive)
            {
                return false;
            }
            //冷却时间
            if(currentPlayerState.FireCooldownTicks > 0)
            {
                return false;
            }
            //瞄准方向
            if(inputFrame.AimX == 0 && inputFrame.AimY == 0 && currentPlayerState.AimDirection == FixVector3.Zero)
            {
                return false;
            }
            return inputFrame.FirePressed;
        }

        public static PlayerSimState Step(in PlayerSimState currentPlayerState, in InputFrame inputFrame, in SimulationConfig config)
        {
            // 瞄准方向
            Fix64 aimX = (Fix64)inputFrame.AimX/1000;
            Fix64 aimY = (Fix64)inputFrame.AimY/1000;
            FixVector3 aimDirection = new FixVector3(aimX, aimY, Fix64.Zero);
            if(aimX == Fix64.Zero && aimY == Fix64.Zero)
            {
                aimDirection = currentPlayerState.AimDirection;
            }

            FixVector2 moveInput = new FixVector2(inputFrame.MoveX, inputFrame.MoveY);
            if(moveInput != FixVector2.Zero)
            {
                moveInput.Normalize();
            }

            // 位置变化
            FixVector3 deltaMove = new FixVector3(moveInput.x, moveInput.y, Fix64.Zero) * config.PlayerMoveSpeed * config.TickDeltaTime;
            FixVector3 nextPosition = currentPlayerState.Position + deltaMove;

            //限制玩家位置
            Fix64 clampedX = nextPosition.x;
            if (clampedX < config.PlayAreaMin.x) clampedX = config.PlayAreaMin.x;
            if (clampedX > config.PlayAreaMax.x) clampedX = config.PlayAreaMax.x;            
            Fix64 clampedY = nextPosition.y;
            if (clampedY < config.PlayAreaMin.y) clampedY = config.PlayAreaMin.y;
            if (clampedY > config.PlayAreaMax.y) clampedY = config.PlayAreaMax.y;
            nextPosition = new FixVector3(clampedX, clampedY, currentPlayerState.Position.z);

            
            int nextCooldown = currentPlayerState.FireCooldownTicks > 0
                ? currentPlayerState.FireCooldownTicks - 1
                : 0;
            if(ShouldFire(currentPlayerState, inputFrame))
            {
                nextCooldown = config.PlayerFireIntervalTicks;
            }
            return new PlayerSimState(
                currentPlayerState.EntityId,
                nextPosition,
                aimDirection,
                currentPlayerState.Hp,
                currentPlayerState.IsAlive,
                nextCooldown,
                currentPlayerState.RespawnCountdownTicks,
                currentPlayerState.InvincibleTicks
            );

        }

    }



}