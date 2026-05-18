using BulletHell.Simulation.Core;

[System.Serializable]
public struct ReplayFrame
{
    public int Tick;
    public FrameInputBundle Input;
    public ulong WorldHash;
}