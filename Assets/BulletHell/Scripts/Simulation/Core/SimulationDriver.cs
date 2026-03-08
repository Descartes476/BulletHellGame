using System.Collections;
using System.Collections.Generic;
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
            new FixVector2((Fix64)initialPosition.x, (Fix64)initialPosition.y),
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
            InputFrame inputFrame = playerController.SampleCurrentInputFrame(_worldTick);
            _currentWorld = WorldSimulator.Step(_currentWorld, inputFrame);
            _worldTick = _currentWorld.Tick;
            _accumulator -= tickInterval;
        }

        FixVector2 simulatedPosition = _currentWorld.Player.Position;
        Vector3 playerPosition = playerController.transform.position;
        playerPosition.x = (float)simulatedPosition.x;
        playerPosition.y = (float)simulatedPosition.y;
        playerPosition.z = 0f;
        playerController.transform.position = playerPosition;
    }
}
