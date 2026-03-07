namespace BulletHell.Simulation.Core
{
    public readonly struct InputFrame
    {
        public int Tick { get; }
        public sbyte MoveX { get; }
        public sbyte MoveY { get; }
        public short AimX { get; }
        public short AimY { get; }
        public bool FirePressed { get; }

        public bool HasMoveInput => MoveX != 0 || MoveY != 0;

        public InputFrame(int tick, sbyte moveX, sbyte moveY, short aimX, short aimY, bool firePressed)
        {
            Tick = tick;
            MoveX = moveX;
            MoveY = moveY;
            AimX = aimX;
            AimY = aimY;
            FirePressed = firePressed;
        }
    }
}
