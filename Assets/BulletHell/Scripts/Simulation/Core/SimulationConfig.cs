namespace BulletHell.Simulation.Core
{
    public readonly struct SimulationConfig
    {
        public int TickRate { get; }
        public Fix64 PlayerMoveSpeed { get; }
        public int PlayerFireIntervalTicks { get; }
        public Fix64 PlayerBulletSpeed { get; }
        public Fix64 PlayerBulletDamage { get; }
        public int PlayerBulletLifetimeTicks { get; }
        public FixVector2 PlayAreaMin { get; }
        public FixVector2 PlayAreaMax { get; }
        public Fix64 TickDeltaTime => TickRate > 0 ? Fix64.One / TickRate : Fix64.Zero;

        public SimulationConfig(
            int tickRate,
            Fix64 playerMoveSpeed,
            int playerFireIntervalTicks,
            Fix64 playerBulletSpeed,
            Fix64 playerBulletDamage,
            int playerBulletLifetimeTicks,
            FixVector2 playAreaMin,
            FixVector2 playAreaMax)
        {
            TickRate = tickRate;
            PlayerMoveSpeed = playerMoveSpeed;
            PlayerFireIntervalTicks = playerFireIntervalTicks;
            PlayerBulletSpeed = playerBulletSpeed;
            PlayerBulletDamage = playerBulletDamage;
            PlayerBulletLifetimeTicks = playerBulletLifetimeTicks;
            PlayAreaMin = playAreaMin;
            PlayAreaMax = playAreaMax;
        }
    }
}
