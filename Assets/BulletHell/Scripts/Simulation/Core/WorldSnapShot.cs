namespace BulletHell.Simulation.Core
{
    public readonly struct WorldSnapshot
    {
        public int Tick { get; }
        public SimulationConfig Config { get; }
        public PlayerSimState Player { get; }

        public BulletSimState[] Bullets { get; }

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