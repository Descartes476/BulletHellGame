namespace BulletHell.Simulation.Core
{
    public readonly struct WorldSnapshot
    {
        /// <summary>
        /// 当前世界快照对应的逻辑 Tick 编号。
        /// </summary>
        public int Tick { get; }
        /// <summary>
        /// 当前世界快照使用的仿真配置。
        /// </summary>
        public SimulationConfig Config { get; }
        /// <summary>
        /// 当前世界中的玩家状态。
        /// </summary>
        public PlayerSimState Player { get; }

        /// <summary>
        /// 当前世界中的所有子弹状态。
        /// </summary>
        public BulletSimState[] Bullets { get; }

        /// <summary>
        /// 当前世界中的所有敌人状态。
        /// </summary>
        public EnemySimState[] Enemies { get; }

        public WorldSnapshot(int tick, SimulationConfig config, PlayerSimState player, BulletSimState[] bullets, EnemySimState[] enemies)
        {
            Tick = tick;
            Config = config;
            Player = player;
            Bullets = bullets == null ? new BulletSimState[0] : (BulletSimState[])bullets.Clone();
            Enemies = enemies == null ? new EnemySimState[0] : (EnemySimState[])enemies.Clone();
        }
    }
}