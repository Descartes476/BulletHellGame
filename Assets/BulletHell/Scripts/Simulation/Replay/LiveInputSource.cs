using BulletHell.Simulation.Core;

public sealed class LiveInputSource : IInputSource
{
    private PlayerController playerController;

    public LiveInputSource(PlayerController playerController)
    {
        if(playerController == null)
        {
            throw new System.ArgumentNullException(nameof(playerController));
        }

        this.playerController = playerController;
    }

    public bool TryGetInput(int tick, out InputFrame inputFrame)
    {
        if(playerController == null)
        {
            inputFrame = default;
            return false;
        }

        inputFrame = playerController.SampleCurrentInputFrame(tick);
        return true;
    }
}