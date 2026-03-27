namespace BulletHell.Simulation.Core
{
    public readonly struct SimulationConfig
    {
        // 玩家参数
        public int TickRate { get; }  // 帧率
        public Fix64 PlayerMoveSpeed { get; }  // 玩家移动速度
        public int PlayerFireIntervalTicks { get; } // 玩家射击冷却
        public Fix64 PlayerBulletSpeed { get; } // 玩家子弹速度
        public Fix64 PlayerBulletDamage { get; } // 玩家子弹伤害
        public int PlayerBulletLifetimeTicks { get; } // 玩家子弹寿命
        public Fix64 PlayerBulletHitRadius { get; } // 子弹碰撞范围
        public Fix64 PlayerHitRadius { get; } // 玩家受击范围
        public Fix64 PlayerMaxHp { get; }  // 玩家最大血量
        public int PlayerRespawnTicks { get; }  // 玩家复活倒计时
        public int PlayerInvincibleTicks { get; }  // 玩家无敌时间

        public FixVector2 PlayAreaMin { get; }
        public FixVector2 PlayAreaMax { get; }

        // 敌人参数
        public int EnemyFireIntervalTicks { get; } // 敌人射击冷却
        public Fix64 EnemyBulletSpeed { get; } // 敌人子弹速度
        public Fix64 EnemyBulletDamage{ get; } // 敌人子弹伤害
        public int EnemyBulletLifetimeTicks { get; } // 敌人子弹寿命
        public Fix64 EnemyBulletHitRadius { get; } // 敌人子弹碰撞范围
        public Fix64 EnemyMoveSpeed { get; } // 敌人移动速度
        public Fix64 TickDeltaTime => TickRate > 0 ? Fix64.One / TickRate : Fix64.Zero;

        public SimulationConfig(
            int tickRate,
            Fix64 playerMoveSpeed,
            int playerFireIntervalTicks,
            Fix64 playerBulletSpeed,
            Fix64 playerBulletDamage,
            int playerBulletLifetimeTicks,
            Fix64 playerBulletHitRadius,
            Fix64 playerHitRadius,
            Fix64 playerMaxHp,
            int playerRespawnTicks,
            int playerInvincibleTicks,
            FixVector2 playAreaMin,
            FixVector2 playAreaMax,
            int enemyFireIntervalTicks,
            Fix64 enemyBulletSpeed,
            Fix64 enemyBulletDamage,
            int enemyBulletLifetimeTicks,
            Fix64 enemyBulletHitRadius,
            Fix64 enemyMoveSpeed
        )
        {
            TickRate = tickRate;
            //Player参数
            PlayerMoveSpeed = playerMoveSpeed;
            PlayerFireIntervalTicks = playerFireIntervalTicks;
            PlayerBulletSpeed = playerBulletSpeed;
            PlayerBulletDamage = playerBulletDamage;
            PlayerBulletLifetimeTicks = playerBulletLifetimeTicks;
            PlayerBulletHitRadius = playerBulletHitRadius;
            PlayerHitRadius = playerHitRadius;
            PlayerMaxHp = playerMaxHp;
            PlayerRespawnTicks = playerRespawnTicks;
            PlayerInvincibleTicks = playerInvincibleTicks;
            PlayAreaMin = playAreaMin;
            PlayAreaMax = playAreaMax;

            //Enemy参数
            EnemyFireIntervalTicks = enemyFireIntervalTicks;
            EnemyBulletSpeed = enemyBulletSpeed;
            EnemyBulletDamage = enemyBulletDamage;
            EnemyBulletLifetimeTicks = enemyBulletLifetimeTicks;
            EnemyBulletHitRadius = enemyBulletHitRadius;
            EnemyMoveSpeed = enemyMoveSpeed;
        }
    }
}
