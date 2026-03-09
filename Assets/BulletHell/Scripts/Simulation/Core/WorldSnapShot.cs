namespace BulletHell.Simulation.Core
{
    public readonly struct WorldSnapshot
    {
        public int Tick { get; }
        public SimulationConfig Config { get; }
        public PlayerSimState Player { get; }

        public BulletSimState[] Bullets { get; }

        public WorldSnapshot(int tick, SimulationConfig config, PlayerSimState player, BulletSimState[] bullets)
        {
            Tick = tick;
            Config = config;
            Player = player;
            Bullets = bullets == null ? new BulletSimState[0] : (BulletSimState[])bullets.Clone();
        }
    }
}