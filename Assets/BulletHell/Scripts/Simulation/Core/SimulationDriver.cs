using UnityEngine;
using BulletHell.Simulation.Core;
using System;
using System.Collections.Generic;
using System.Linq;

public class SimulationDriver : MonoBehaviour
{
    [SerializeField] private SimulationConfigAsset defaultConfigAsset;
    [SerializeField] private PlayerController playerController;

    private SimulationConfig config;
    private WorldSnapshot _currentWorld;
    private float _accumulator;
    private int _worldTick;
    private int _nextBulletEntityID = 1;

    private Dictionary<int, Bullet> _bulletViews = new Dictionary<int, Bullet>();

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
        _currentWorld = new WorldSnapshot(_worldTick, config, initialPlayer, new BulletSimState[0], new EnemySimState[0]);
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
            if(shouldFire)  // 生成新子弹
            {
                int bulletEntityID = _nextBulletEntityID++;
                FixVector3 bulletPosition = nextWorld.Player.Position;
                FixVector2 fallbackDirection = _currentWorld.Player.AimDirection.GetNormalizedOr(new FixVector2(Fix64.Zero, Fix64.One));
                FixVector2 bulletDirection = nextWorld.Player.AimDirection.GetNormalizedOr(fallbackDirection);

                BulletSimState bullet = new BulletSimState(
                    bulletEntityID,
                    bulletPosition,
                    bulletDirection,
                    config.PlayerBulletSpeed,
                    config.PlayerBulletDamage,
                    (Fix64)0.1f,
                    config.PlayerBulletLifetimeTicks,
                    BulletFaction.Player
                );
                var nextWorldBullets = nextWorld.Bullets;
                BulletSimState[] newBullets = new BulletSimState[nextWorldBullets.Length + 1];
                Array.Copy(nextWorldBullets, newBullets, nextWorldBullets.Length);
                newBullets[nextWorldBullets.Length] = bullet;
                nextWorld = new WorldSnapshot(nextWorld.Tick, nextWorld.Config, nextWorld.Player, newBullets, nextWorld.Enemies);
            }
            nextWorld = ResolvePlayerBulletHits(nextWorld);
            _currentWorld = nextWorld;
            _worldTick = _currentWorld.Tick;
            #endregion

            _accumulator -= tickInterval;
        }

        FixVector3 simulatedPosition = _currentWorld.Player.Position;
        playerController.transform.position = simulatedPosition.ToVector3();
        
        SynBulletsView(_currentWorld.Bullets);
    }

    private void SynBulletsView(BulletSimState[] bullets)
    {
        if (BulletManager.Instance == null)
        {
            return;
        }

        HashSet<int> activeBulletIds = new HashSet<int>();
        foreach (var bullet in bullets)
        {
            activeBulletIds.Add(bullet.EntityId);
            if (!_bulletViews.ContainsKey(bullet.EntityId))
            {
                Bullet bulletView = BulletManager.Instance.SpawnBullet(
                    bullet.Position.ToVector3(),
                    bullet.Direction.ToVector2(),
                    (float)bullet.Speed,
                    (float)bullet.Damage,
                    bullet.RemainingLifetimeTicks * (1.0f / config.TickRate),
                    bullet.Faction
                );
                if (bulletView != null)
                {
                    _bulletViews[bullet.EntityId] = bulletView;
                }
            }
            else
            {
                Bullet bulletView = _bulletViews[bullet.EntityId];
                if (bulletView == null)
                {
                    _bulletViews.Remove(bullet.EntityId);
                    continue;
                }

                bulletView.transform.position = bullet.Position.ToVector3();
            }

        }

        int[] toRemove = _bulletViews.Keys.Where(id => !activeBulletIds.Contains(id)).ToArray();
        foreach (var id in toRemove)
        {
            Bullet bulletView = _bulletViews[id];
            if (bulletView != null)
            {
                BulletManager.Instance.RecycleBullet(bulletView);
            }

            _bulletViews.Remove(id);
        }
        
    }

    private WorldSnapshot ResolvePlayerBulletHits(WorldSnapshot world)
    {
        var bulletSims = world.Bullets;
        List<BulletSimState> bulletsNotHit = new List<BulletSimState>();
        for(int i = 0; i < bulletSims.Length; i++)
        {
            BulletSimState bullet = bulletSims[i];
            if(!TryHitEnemy(bullet))
            {
                bulletsNotHit.Add(bullet);
            }
        }
        WorldSnapshot worldSnapshot = new WorldSnapshot
        (
            world.Tick,
            world.Config,
            world.Player,
            bulletsNotHit.ToArray(),
            world.Enemies
        );
        return worldSnapshot;
    }

    private static bool TryHitEnemy(BulletSimState bullet)
    {
        var enemies = EnemyBase.ActiveEnemies;
        if (enemies == null || enemies.Count == 0)
            return false;

        FixVector3 bulletPos3 = bullet.Position;
        FixVector2 bulletPos = new FixVector2(bulletPos3.x, bulletPos3.y);
        Fix64 bulletR = bullet.Radius;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled)
                continue;

            Vector3 enemyPos3 = enemy.transform.position;
            FixVector2 enemyPos = new FixVector2((Fix64)enemyPos3.x, (Fix64)enemyPos3.y);

            Fix64 r = bulletR + enemy.HitRadius;
            FixVector2 d = enemyPos - bulletPos;
            if (FixVector2.SqrMagnitude(d) <= r * r)
            {
                return true;
            }
        }

        return false;
    }
}
