namespace BulletHell.Simulation.Core
{
    public readonly struct FrameInputBundle
    {
        public int Tick { get; }
        public InputFrame LocalInput { get; }
        public InputFrame RemoteInput { get; }

        public FrameInputBundle(int tick, InputFrame localInput, InputFrame remoteInput)
        {
            Tick = tick;
            LocalInput = localInput;
            RemoteInput = remoteInput;
        }
    }
}