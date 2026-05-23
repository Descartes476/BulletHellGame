using System;
using System.Collections.Generic;
using BulletHell.Simulation.Core;

public class DeterministicSimulationRunner
{
    private SimulationConfig _config;
    private WorldSnapshot _currentWorld;
    private DeterministicRandom _random;
    int _nextBulletEntityID;
    private readonly List<int> _enemyDiedEntityIds = new List<int>();
    public ulong CurrentHash;
    
    int CurrentTick => _currentWorld.Tick;

    public DeterministicSimulationRunner(WorldSnapshot world, SimulationConfig config, uint seed)
    {
        _currentWorld = world;
        _config = config;
        _nextBulletEntityID = 1;
        CurrentHash = WorldStateHasher.Compute(world);
        _random = new DeterministicRandom(seed);
    }

    public WorldSnapshot CurrentWorld
    {
        get => _currentWorld;
    }

    public int Tick
    {
        get => _currentWorld.Tick;
    }

    public IReadOnlyList<int> EnemyDiedEntityIds
    {
        get => _enemyDiedEntityIds;
    }

    public void Step(FrameInputBundle inputBundle)
    {
        _enemyDiedEntityIds.Clear();
        // 推进输入与世界状态
        WorldSnapshot nextWorld = ResolveInput(_currentWorld, inputBundle);
        // 生成敌方子弹
        nextWorld = ResolveEnemyFire(nextWorld);
        // 结算玩家子弹命中
        nextWorld = ResolvePlayerBulletHits(nextWorld);
        // 结算敌方子弹命中
        nextWorld = ResolveEnemyBulletHits(nextWorld);
        _currentWorld = nextWorld;
        CurrentHash = WorldStateHasher.Compute(_currentWorld);
    }

    private WorldSnapshot ResolveInput(WorldSnapshot world, FrameInputBundle inputBundle)
    {        
        WorldSnapshot nextWorld = WorldSimulator.Step(world, inputBundle, _random);

        List<BulletSimState> bullets = new List<BulletSimState>();
        bullets.AddRange(nextWorld.Bullets);

        for(int i = 0; i < world.Players.Length; i++)
        {
            PlayerSimState oldPlayer = world.Players[i];
            PlayerSimState nextPlayer = nextWorld.Players[i];
            InputFrame input = GetInputForPlayerIndex(inputBundle, i);

            if (!PlayerSimulator.ShouldFire(oldPlayer, input))
            {
                continue;
            }

            int bulletEntityID = _nextBulletEntityID++;
            FixVector3 bulletPosition = nextPlayer.Position;
            FixVector3 fallbackDirection = oldPlayer.AimDirection.GetNormalizedOr(new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero));
            FixVector3 bulletDirection = nextPlayer.AimDirection.GetNormalizedOr(fallbackDirection);

            bullets.Add(new BulletSimState(
                bulletEntityID,
                bulletPosition,
                bulletDirection,
                _config.PlayerBulletSpeed,
                _config.PlayerBulletDamage,
                _config.PlayerBulletHitRadius,
                _config.PlayerBulletLifetimeTicks,
                BulletFaction.Player,
                oldPlayer.EntityId
            ));
        }

        return new WorldSnapshot(
            nextWorld.Tick,
            nextWorld.Config,
            nextWorld.Players,
            bullets.ToArray(),
            nextWorld.Enemies
        );
    }
    

    private static InputFrame GetInputForPlayerIndex(FrameInputBundle inputBundle, int playerIndex)
    {
        return playerIndex == 0 ? inputBundle.P1Input : inputBundle.P2Input;
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
                    _config.EnemyBulletSpeed,
                    _config.EnemyBulletDamage,
                    _config.EnemyBulletHitRadius,
                    _config.EnemyBulletLifetimeTicks,
                    BulletFaction.Enemy,
                    0
                );
                bulletSims.Add(bullet);
                coolDownTick = _config.EnemyFireIntervalTicks;
            }
            enemies[i] = new EnemySimState(
                enemy.EntityId,
                enemy.Position,
                enemy.MoveDirection,
                enemy.Hp,
                enemy.MaxHp,
                enemy.HitRadius,
                enemy.Speed,
                coolDownTick,
                enemy.MoveDecisionCooldownTicks
            );
        }
        return new WorldSnapshot(
            world.Tick,
            world.Config,
            world.Players,
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
            world.Players,
            bulletsNotHit.ToArray(),
            enemies
        );
        return worldSnapshot;
    }

    // 处理敌方子弹对玩家的命中结算，并移除命中的敌方子弹
    private WorldSnapshot ResolveEnemyBulletHits(WorldSnapshot world)
    {
        BulletSimState[] bulletSims = world.Bullets;
        PlayerSimState[] players = (PlayerSimState[])world.Players.Clone();
        List<BulletSimState> bulletsNotHit = new List<BulletSimState>();
        for(int i=0; i<bulletSims.Length; i++)
        {
            BulletSimState bullet = bulletSims[i];
            if(bullet.Faction != BulletFaction.Enemy)
            {
                bulletsNotHit.Add(bullet);
                continue;
            }

            bool hitPlayer = false;
            for(int playerIndex = 0; playerIndex < players.Length; playerIndex++)
            {
                PlayerSimState player = players[playerIndex];
                if(!player.IsAlive)
                {
                    continue;
                }

                // 计算玩家受击
                FixVector3 playerPos = player.Position;
                FixVector3 bulletPos = bullet.Position;   
                FixVector3 d = playerPos - bulletPos;
                Fix64 sqrDistanceInPanel = d.x * d.x + d.y * d.y;
                Fix64 r = bullet.Radius + player.HitRadius;
                if (sqrDistanceInPanel <= r * r)
                {
                    Fix64 nextHp = player.Hp;
                    int respawnCountdownTicks = player.RespawnCountdownTicks;
                    int invincibleTicks = player.InvincibleTicks;
                    bool isAlive = player.IsAlive;
                    if(!player.IsInvincible)
                    {
                        nextHp -= bullet.Damage;
                        if(nextHp <= Fix64.Zero)
                        {
                            isAlive = false;
                            nextHp = Fix64.Zero;
                            respawnCountdownTicks = _config.PlayerRespawnTicks;
                            invincibleTicks = 0;
                        }
                    }

                    players[playerIndex] = new PlayerSimState(
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

                    hitPlayer = true;
                    break;
                }
            }

            if(!hitPlayer)
            {
                bulletsNotHit.Add(bullet);
            }
        }

        return new WorldSnapshot(
            world.Tick,
            world.Config,
            players,
            bulletsNotHit.ToArray(),
            world.Enemies
        );
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
                    _enemyDiedEntityIds.Add(enemy.EntityId);
                }
                enemies[i] = new EnemySimState(
                    enemy.EntityId,
                    enemy.Position,
                    enemy.MoveDirection,
                    nextHp,
                    enemy.MaxHp,
                    enemy.HitRadius,
                    enemy.Speed,
                    enemy.FireCooldownTicks,
                    enemy.MoveDecisionCooldownTicks
                );
                return true;
            }
        }

        return false;
    }
}
