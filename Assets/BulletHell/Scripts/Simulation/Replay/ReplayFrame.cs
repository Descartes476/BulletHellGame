using BulletHell.Simulation.Core;

[System.Serializable]
public struct ReplayFrame
{
    public int Tick;
    public InputFrame Input;
    public ulong WorldHash;
}