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
    private int _nextEnemyEntityID = 1;

    private Dictionary<int, Bullet> _bulletViews = new Dictionary<int, Bullet>();
    private Dictionary<int, EnemyBase> _enemyViews = new Dictionary<int, EnemyBase>();
    private HashSet<int> _enemyDieInTick = new HashSet<int>();

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
        _currentWorld = new WorldSnapshot(_worldTick, config, initialPlayer, new BulletSimState[0], GetEnemySimStates());
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
            ResolveEnemyDied();
        }

        FixVector3 simulatedPosition = _currentWorld.Player.Position;
        playerController.transform.position = simulatedPosition.ToVector3();
        
        SynBulletsView(_currentWorld.Bullets);
        SyncEnemyView(_currentWorld.Enemies);
    }

    private void ResolveEnemyDied()
    {
        foreach(var enemyId in _enemyDieInTick)
        {
            if(GameManager.Instance != null)
            {
                GameManager.Instance.TriggerEnemyDied(enemyId);
            }
        }
        _enemyDieInTick.Clear();
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

    private void SyncEnemyView(EnemySimState[] enemies)
    {
        for(int i=0; i<enemies.Length; i++)
        {
            var enemy = enemies[i];
            if(!_enemyViews.TryGetValue(enemy.EntityId, out EnemyBase enemyView))
            {
                continue;
            }
            if(enemyView == null)
            {
                continue;
            }
            if(!enemy.IsAlive)
            {
                if(enemyView.gameObject.activeSelf)
                {
                    enemyView.gameObject.SetActive(false);
                }
                continue;
            }
            enemyView.transform.position = enemy.Position.ToVector3();
        }
    }

    private WorldSnapshot ResolvePlayerBulletHits(WorldSnapshot world)
    {
        var bulletSims = world.Bullets;
        var enemies = (EnemySimState[])world.Enemies.Clone();
        List<BulletSimState> bulletsNotHit = new List<BulletSimState>();
        for(int i = 0; i < bulletSims.Length; i++)
        {
            BulletSimState bullet = bulletSims[i];
            if (bullet.Faction != BulletFaction.Player || !TryHitEnemy(bullet, enemies))
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
            enemies
        );
        return worldSnapshot;
    }

    private EnemySimState[] GetEnemySimStates()
    {
        EnemyBase[] sceneEnemies = FindObjectsOfType<EnemyBase>();
        Array.Sort(sceneEnemies, (a, b) => string.CompareOrdinal(GetTransformPath(a.transform), GetTransformPath(b.transform)));
        List<EnemySimState> enemySimStates = new List<EnemySimState>();
        _enemyViews.Clear();
        for(int i=0; i<sceneEnemies.Length; i++)
        {
            EnemyBase enemy = sceneEnemies[i];
            Vector3 pos = enemy.transform.position;
            FixVector3 fixPos = new FixVector3((Fix64)pos.x, (Fix64)pos.y, (Fix64)pos.z);
            int enemyEntityId = _nextEnemyEntityID;
            EnemySimState enemySimState = new EnemySimState(enemyEntityId, fixPos, (Fix64)enemy.MaxHp, (Fix64)enemy.MaxHp, (Fix64)enemy.HitRadius);
            enemySimStates.Add(enemySimState);
            _enemyViews[enemyEntityId] = enemy;
            _nextEnemyEntityID++;
        }
        return enemySimStates.ToArray();
    }

    private static string GetTransformPath(Transform current)
    {
        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    private bool TryHitEnemy(BulletSimState bullet, EnemySimState[] enemies)
    {
        if (enemies == null || enemies.Length == 0)
            return false;

        FixVector3 bulletPos = bullet.Position;
        Fix64 bulletR = bullet.Radius;

        for (int i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            if (!enemy.IsAlive)
            {
                continue;
            }

            FixVector3 enemyPos = enemy.Position;

            Fix64 r = bulletR + enemy.HitRadius;
            FixVector3 d = enemyPos - bulletPos;
            FixVector2 dInPanel = new FixVector2(d.x, d.y);
            if (FixVector2.SqrMagnitude(dInPanel) <= r * r)
            {
                var nextHp = enemy.Hp;
                nextHp -= bullet.Damage;
                if(nextHp <= Fix64.Zero)
                {
                    nextHp = Fix64.Zero;
                    _enemyDieInTick.Add(enemy.EntityId);
                }
                enemies[i] = new EnemySimState(
                    enemy.EntityId,
                    enemy.Position,
                    nextHp,
                    enemy.MaxHp,
                    enemy.HitRadius
                );
                return true;
            }
        }

        return false;
    }

}
