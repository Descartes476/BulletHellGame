namespace BulletHell.Simulation.Core
{
    public static class WorldSimulator
    {
        public static WorldSnapshot Step(in WorldSnapshot currentWorld, in InputFrame playerInput)
        {
            PlayerSimState nextPlayerState = PlayerSimulator.Step(currentWorld.Player, playerInput, currentWorld.Config);
            int nextTick = currentWorld.Tick + 1;

            return new WorldSnapshot(nextTick, currentWorld.Config, nextPlayerState);
        }
    }
}
