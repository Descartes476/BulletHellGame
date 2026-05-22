namespace BulletHell.Simulation.Core
{
    public readonly struct FrameInputBundle
    {
        public int Tick { get; }
        public InputFrame P1Input { get; }
        public InputFrame P2Input { get; }

        public FrameInputBundle(int tick, InputFrame p1Input, InputFrame p2Input)
        {
            Tick = tick;
            P1Input = p1Input;
            P2Input = p2Input;
        }
    }
}