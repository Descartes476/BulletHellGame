namespace BulletHell.Simulation.Core
{
    public readonly struct WorldSnapshot
    {
        public int Tick { get; }
        public SimulationConfig Config { get; }
        public PlayerSimState Player { get; }

        public WorldSnapshot(int tick, SimulationConfig config, PlayerSimState player)
        {
            Tick = tick;
            Config = config;
            Player = player;
        }
    }
}