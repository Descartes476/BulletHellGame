using UnityEngine;
using BulletHell.Simulation.Core;

public class SimulationDriver : MonoBehaviour
{
    [SerializeField] private SimulationConfigAsset defaultConfigAsset;
    [SerializeField] private PlayerController playerController;

    private SimulationConfig config;
    private WorldSnapshot _currentWorld;
    private float _accumulator;
    private int _worldTick;

    // Start is called before the first frame update
    void Start()
    {
        _worldTick = 0;
        _accumulator = 0f;

        if (defaultConfigAsset == null)
        {
            Debug.LogError("SimulationDriver: Default config asset is not assigned.");
            enabled = false;
            return;
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        if (playerController == null)
        {
            Debug.LogError("SimulationDriver: PlayerController was not found.");
            enabled = false;
            return;
        }

        config = defaultConfigAsset.ToSimulationConfig();
        playerController.SetSimulationDriven(true);

        Vector3 initialPosition = playerController.transform.position;
        PlayerSimState initialPlayer = new PlayerSimState(
            1,
            new FixVector3((Fix64)initialPosition.x, (Fix64)initialPosition.y, (Fix64)initialPosition.z),
            new FixVector2(Fix64.Zero, Fix64.One),
            (Fix64)1,
            true,
            0,
            0,
            0);
        _currentWorld = new WorldSnapshot(_worldTick, config, initialPlayer);
    }

    // Update is called once per frame
    void Update()
    {
        if (config.TickRate <= 0)
            return;

        float tickInterval = 1f / config.TickRate;
        _accumulator += Time.deltaTime;

        while (_accumulator >= tickInterval)
        {
            #region Tick步进前，获取必要的状态
            InputFrame inputFrame = playerController.SampleCurrentInputFrame(_worldTick);
            bool shouldFire = PlayerSimulator.ShouldFire(_currentWorld.Player, inputFrame);
            #endregion

            #region Tick步进
            WorldSnapshot nextWorld = WorldSimulator.Step(_currentWorld, inputFrame);
            if (shouldFire)
            {
                PlayerSimState nextPlayerState = nextWorld.Player;
                if (BulletManager.Instance != null)
                {
                    BulletManager.Instance.SpawnBullet(nextPlayerState.Position.ToVector3(), nextPlayerState.AimDirection.ToVector2(), (float)config.PlayerBulletSpeed, (float)config.PlayerBulletDamage, config.PlayerBulletLifetimeTicks * tickInterval, BulletFaction.Player);
                }
            }
            _currentWorld = nextWorld;
            _worldTick = _currentWorld.Tick;
            #endregion

            _accumulator -= tickInterval;
        }

        FixVector3 simulatedPosition = _currentWorld.Player.Position;
        playerController.transform.position = simulatedPosition.ToVector3();
    }
}
