using System.Collections.Generic;

[System.Serializable]
public sealed class ReplayData
{
    public int Version;
    public int TickRate;
    public int Seed;
    public ReplayConfigSnapshot ConfigSnapshot;
    public List<ReplayFrame> Frames;
}