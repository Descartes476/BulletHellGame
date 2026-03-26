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
            new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero),
            config.PlayerMaxHp,
            config.PlayerHitRadius,
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
            // 推进输入与世界状态
            WorldSnapshot nextWorld = ResolveInput(_currentWorld);
            // 生成敌方子弹
            nextWorld = ResolveEnemyFire(nextWorld);
            // 结算玩家子弹命中
            nextWorld = ResolvePlayerBulletHits(nextWorld);
            // 结算敌方子弹命中
            nextWorld = ResolveEnemyBulletHits(nextWorld);
            _currentWorld = nextWorld;
            _worldTick = _currentWorld.Tick;

            _accumulator -= tickInterval;
            ResolveEnemyDied();
        }

        FixVector3 simulatedPosition = _currentWorld.Player.Position;
        playerController.transform.position = simulatedPosition.ToVector3();
        
        SyncBulletViews(_currentWorld.Bullets);
        SyncEnemyView(_currentWorld.Enemies);
    }

    //触发敌人死亡事件
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

    private void SyncBulletViews(BulletSimState[] bullets)
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
                    new Vector3((float)bullet.Direction.x, (float)bullet.Direction.y, (float)bullet.Direction.z),
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

    private WorldSnapshot ResolveInput(WorldSnapshot world)
    {
        InputFrame inputFrame = playerController.SampleCurrentInputFrame(_worldTick);
        bool shouldFire = PlayerSimulator.ShouldFire(world.Player, inputFrame);
        WorldSnapshot nextWorld = WorldSimulator.Step(world, inputFrame);
        if(shouldFire)  // 生成新子弹
        {
            int bulletEntityID = _nextBulletEntityID++;
            FixVector3 bulletPosition = nextWorld.Player.Position;
            FixVector3 fallbackDirection = world.Player.AimDirection.GetNormalizedOr(new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero));
            // 归一化，目标太近则给一个默认方向
            FixVector3 bulletDirection = nextWorld.Player.AimDirection.GetNormalizedOr(fallbackDirection);

            BulletSimState bullet = new BulletSimState(
                bulletEntityID,
                bulletPosition,
                bulletDirection,
                config.PlayerBulletSpeed,
                config.PlayerBulletDamage,
                config.PlayerBulletHitRadius,
                config.PlayerBulletLifetimeTicks,
                BulletFaction.Player
            );
            var nextWorldBullets = nextWorld.Bullets;
            BulletSimState[] newBullets = new BulletSimState[nextWorldBullets.Length + 1];
            Array.Copy(nextWorldBullets, newBullets, nextWorldBullets.Length);
            newBullets[nextWorldBullets.Length] = bullet;
            nextWorld = new WorldSnapshot(nextWorld.Tick, nextWorld.Config, nextWorld.Player, newBullets, nextWorld.Enemies);
        }
        return nextWorld;
    }

    // 敌人开火生成子弹
    private WorldSnapshot ResolveEnemyFire(WorldSnapshot world)
    {
        EnemySimState[] enemies = (EnemySimState[])world.Enemies.Clone();
        List<BulletSimState> bulletSims = new List<BulletSimState>();
        bulletSims.AddRange(world.Bullets);
        for(int i=0; i<enemies.Length; i++)
        {
            var enemy = enemies[i];
            if(!enemy.IsAlive)
            {
                continue;
            }
            int coolDownTick = enemy.FireCooldownTicks;
            if(enemy.CanFire)
            {
                int bulletEntityID = _nextBulletEntityID++;
                FixVector3 bulletPosition = enemy.Position;
                FixVector3 aimDirection = world.Player.Position - enemy.Position;
                aimDirection = aimDirection.GetNormalizedOr(new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero));
                BulletSimState bullet = new BulletSimState(
                    bulletEntityID,
                    bulletPosition,
                    aimDirection,
                    config.EnemyBulletSpeed,
                    config.EnemyBulletDamage,
                    config.EnemyBulletHitRadius,
                    config.EnemyBulletLifetimeTicks,
                    BulletFaction.Enemy
                );
                bulletSims.Add(bullet);
                coolDownTick = config.EnemyFireIntervalTicks;
            }
            enemies[i] = new EnemySimState(
                enemy.EntityId,
                enemy.Position,
                enemy.MoveDirection,
                enemy.Hp,
                enemy.MaxHp,
                enemy.HitRadius,
                enemy.Speed,
                coolDownTick
            );
        }
        return new WorldSnapshot(
            world.Tick,
            world.Config,
            world.Player,
            bulletSims.ToArray(),
            enemies
        );
    }

    private WorldSnapshot ResolvePlayerBulletHits(WorldSnapshot world)
    {
        var bulletSims = world.Bullets;
        var enemies = (EnemySimState[])world.Enemies.Clone();
        List<BulletSimState> bulletsNotHit = new List<BulletSimState>();
        for(int i=0; i<bulletSims.Length; i++)
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

    // 处理敌方子弹对玩家的命中结算，并移除命中的敌方子弹
    private WorldSnapshot ResolveEnemyBulletHits(WorldSnapshot world)
    {
        PlayerSimState player = world.Player;
        if(!player.IsAlive)
        {
            return world;
        }
        BulletSimState[] bulletSims = world.Bullets;
        bool isAlive = player.IsAlive;
        Fix64 nextHp = player.Hp;
        int respawnCountdownTicks = player.RespawnCountdownTicks;
        int invincibleTicks = player.InvincibleTicks;
        List<BulletSimState> bulletsNotHit = new List<BulletSimState>();
        for(int i=0; i<bulletSims.Length; i++)
        {
            BulletSimState bullet = bulletSims[i];
            if(bullet.Faction == BulletFaction.Enemy && isAlive) // 避免循环中死亡还吃子弹
            {
                // 计算玩家受击
                FixVector3 playerPos = player.Position;
                FixVector3 bulletPos = bullet.Position;   
                FixVector3 d = playerPos - bulletPos;
                Fix64 sqrDistanceInPanel = d.x * d.x + d.y * d.y;
                Fix64 r = bullet.Radius + player.HitRadius;
                if (sqrDistanceInPanel <= r * r)
                {
                    if(!player.IsInvincible)
                    {
                        nextHp -= bullet.Damage;
                        if(nextHp <= Fix64.Zero)
                        {
                            isAlive = false;
                            nextHp = Fix64.Zero;
                            respawnCountdownTicks = config.PlayerRespawnTicks;
                            invincibleTicks = 0;
                        }
                    }
                }
                else
                {
                    bulletsNotHit.Add(bullet);
                }
            }
            else
            {
                bulletsNotHit.Add(bullet);
            }
        }
        player = new PlayerSimState(
            player.EntityId,
            player.Position,
            player.AimDirection,
            nextHp,
            player.HitRadius,
            isAlive,
            player.FireCooldownTicks,
            respawnCountdownTicks,
            invincibleTicks
        );
        return new WorldSnapshot(
            world.Tick,
            world.Config,
            player,
            bulletsNotHit.ToArray(),
            world.Enemies
        );
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
            EnemySimState enemySimState = new EnemySimState(enemyEntityId, fixPos, new FixVector3(Fix64.One, Fix64.Zero, Fix64.Zero), (Fix64)enemy.MaxHp, (Fix64)enemy.MaxHp, (Fix64)enemy.HitRadius, (Fix64)1.0, 0);
            enemySimStates.Add(enemySimState);
            _enemyViews[enemyEntityId] = enemy;
            _nextEnemyEntityID++;
        }
        return enemySimStates.ToArray();
    }

    //获取Transform路径，作为唯一标识
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
            Fix64 sqrDistanceInPanel = d.x * d.x + d.y * d.y;
            if (sqrDistanceInPanel <= r * r)
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
                    enemy.MoveDirection,
                    nextHp,
                    enemy.MaxHp,
                    enemy.HitRadius,
                    enemy.Speed,
                    enemy.FireCooldownTicks
                );
                return true;
            }
        }

        return false;
    }

}
