
using BulletHell.Simulation.Core;

public static class WorldStateHasher
{
    public static ulong Compute(in WorldSnapshot world)
    {
        StableHashBuilder builder = new StableHashBuilder(0);

        builder.Add(world.Tick);
        AppendPlayer(ref builder, world.Player);

        BulletSimState[] bullets = world.Bullets;
        builder.Add(bullets != null ? bullets.Length : 0);
        if(bullets != null)
        {
            for(int i = 0; i < bullets.Length; i++)
            {
                AppendBullet(ref builder, bullets[i]);
            }
        }

        EnemySimState[] enemies = world.Enemies;
        builder.Add(enemies != null ? enemies.Length : 0);
        if(enemies != null)
        {
            for(int i = 0; i < enemies.Length; i++)
            {
                AppendEnemy(ref builder, enemies[i]);
            }
        }

        return builder.ToHash();
    }

    private static void AppendPlayer(ref StableHashBuilder builder, in PlayerSimState player)
    {
        builder.Add(player.EntityId);
        builder.Add(player.Position);
        builder.Add(player.AimDirection);
        builder.Add(player.Hp);
        builder.Add(player.HitRadius);
        builder.Add(player.IsAlive);
        builder.Add(player.FireCooldownTicks);
        builder.Add(player.RespawnCountdownTicks);
        builder.Add(player.InvincibleTicks);
    }

    private static void AppendBullet(ref StableHashBuilder builder, in BulletSimState bullet)
    {
        builder.Add(bullet.EntityId);
        builder.Add(bullet.Position);
        builder.Add(bullet.Direction);
        builder.Add(bullet.Speed);
        builder.Add(bullet.Damage);
        builder.Add(bullet.Radius);
        builder.Add(bullet.RemainingLifetimeTicks);
        builder.Add((int)bullet.Faction);
        builder.Add(bullet.IsAlive);
    }

    private static void AppendEnemy(ref StableHashBuilder builder, in EnemySimState enemy)
    {
        builder.Add(enemy.EntityId);
        builder.Add(enemy.Position);
        builder.Add(enemy.MoveDirection);
        builder.Add(enemy.Hp);
        builder.Add(enemy.MaxHp);
        builder.Add(enemy.HitRadius);
        builder.Add(enemy.Speed);
        builder.Add(enemy.FireCooldownTicks);
        builder.Add(enemy.IsAlive);
    }
}
