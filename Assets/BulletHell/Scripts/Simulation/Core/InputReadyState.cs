namespace BulletHell.Simulation.Core
{
    public enum InputReadyState
    {
        Ready,
        MissingLocal,  // 缺少 P1 输入；保留旧名称以兼容现有日志和判断。
        MissingRemote, // 缺少 P2 输入；保留旧名称以兼容现有日志和判断。
        MissingBoth
    }
}