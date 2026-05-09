namespace BulletHell.Simulation.Core
{
    public enum InputPeer
    {
        Local,
        Remote
    }

    public readonly struct PeerInputFrame
    {
        public InputPeer Peer { get; }
        public InputFrame Input { get; }

        public PeerInputFrame(InputPeer peer, InputFrame input)
        {
            Peer = peer;
            Input = input;
        }
    }
}