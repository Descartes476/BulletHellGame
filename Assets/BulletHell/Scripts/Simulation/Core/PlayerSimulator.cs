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

        public static PlayerSimState Step(in PlayerSimState currentPlayer, in InputFrame inputFrame, in SimulationConfig config)
        {
            bool shouldFire = ShouldFire(currentPlayer, inputFrame);
            // 无敌倒计时
            int invincibleTicks = currentPlayer.InvincibleTicks;
            if(invincibleTicks > 0)
            {
                invincibleTicks--;
            }
            // 复活倒计时
            bool isAlive = currentPlayer.IsAlive;
            int respawnCountdownTicks = currentPlayer.RespawnCountdownTicks;
            Fix64 nextHp = currentPlayer.Hp;
            if(!isAlive)
            {
                if(respawnCountdownTicks > 0)
                {
                    respawnCountdownTicks--;
                    if(respawnCountdownTicks <= 0)
                    {

                        isAlive = true;
                        nextHp = config.PlayerMaxHp;
                        invincibleTicks = config.PlayerInvincibleTicks;
                        respawnCountdownTicks = 0;
                    }
                }
            }
            
            

            // 瞄准方向
            Fix64 aimX = (Fix64)inputFrame.AimX/1000;
            Fix64 aimY = (Fix64)inputFrame.AimY/1000;
            FixVector3 aimDirection = new FixVector3(aimX, aimY, Fix64.Zero);
            if(aimX == Fix64.Zero && aimY == Fix64.Zero)
            {
                aimDirection = currentPlayer.AimDirection;
            }

            // 玩家移动
            FixVector3 nextPosition = currentPlayer.Position;
            if(isAlive)
            {
                FixVector2 moveInput = new FixVector2(inputFrame.MoveX, inputFrame.MoveY);
                if(moveInput != FixVector2.Zero)
                {
                    moveInput.Normalize();
                }
                FixVector3 deltaMove = new FixVector3(moveInput.x, moveInput.y, Fix64.Zero) * config.PlayerMoveSpeed * config.TickDeltaTime;
                nextPosition = currentPlayer.Position + deltaMove;
                //限制玩家位置
                Fix64 clampedX = nextPosition.x;
                if (clampedX < config.PlayAreaMin.x) clampedX = config.PlayAreaMin.x;
                if (clampedX > config.PlayAreaMax.x) clampedX = config.PlayAreaMax.x;            
                Fix64 clampedY = nextPosition.y;
                if (clampedY < config.PlayAreaMin.y) clampedY = config.PlayAreaMin.y;
                if (clampedY > config.PlayAreaMax.y) clampedY = config.PlayAreaMax.y;
                nextPosition = new FixVector3(clampedX, clampedY, currentPlayer.Position.z);
            }
            
            // 射击冷却
            int nextCooldown = currentPlayer.FireCooldownTicks > 0
                ? currentPlayer.FireCooldownTicks - 1
                : 0;
            if(shouldFire)
            {
                nextCooldown = config.PlayerFireIntervalTicks;
            }

            
            return new PlayerSimState(
                currentPlayer.EntityId,
                nextPosition,
                aimDirection,
                nextHp,
                currentPlayer.HitRadius,
                isAlive,
                nextCooldown,
                respawnCountdownTicks,
                invincibleTicks
            );
        }
    }



}