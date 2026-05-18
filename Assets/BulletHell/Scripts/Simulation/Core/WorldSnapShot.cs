namespace BulletHell.Simulation.Core
{
    public readonly struct WorldSnapshot
    {
        
        public int Tick { get; } // 当前世界快照对应的逻辑 Tick 编号。
        public SimulationConfig Config { get; } // 当前世界快照使用的仿真配置。
        
        public PlayerSimState[] Players { get; } // 当前世界中的玩家状态。

        public PlayerSimState Player
        {
            get{ return Players.Length > 0 ? Players[0] : default; }
        }

        public BulletSimState[] Bullets { get; } // 当前世界中的所有子弹状态。

        public EnemySimState[] Enemies { get; }// 当前世界中的所有敌人状态。

        public WorldSnapshot(
            int tick,
            SimulationConfig config,
            PlayerSimState player,
            BulletSimState[] bullets,
            EnemySimState[] enemies)
            : this(tick, config, new PlayerSimState[] { player }, bullets, enemies)
        {
            
        }

        public WorldSnapshot(int tick, SimulationConfig config, PlayerSimState[] players, BulletSimState[] bullets, EnemySimState[] enemies)
        {
            Tick = tick;
            Config = config;
            Players = players == null ? new PlayerSimState[0] : (PlayerSimState[])players.Clone();
            Bullets = bullets == null ? new BulletSimState[0] : (BulletSimState[])bullets.Clone();
            Enemies = enemies == null ? new EnemySimState[0] : (EnemySimState[])enemies.Clone();
        }
    }
}