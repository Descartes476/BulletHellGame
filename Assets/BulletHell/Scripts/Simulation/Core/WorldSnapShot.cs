using Unity.VisualScripting.FullSerializer;
using UnityEngine;

namespace BulletHell.Simulation.Core
{
    public readonly struct WorldSnapshot
    {
        
        public int Tick { get; } // 当前世界快照对应的逻辑 Tick 编号。
        public SimulationConfig Config { get; } // 当前世界快照使用的仿真配置。
        
        public PlayerSimState[] Players { get; } // 当前世界中的玩家状态。

        public PlayerSimState Player
        {
            get{ return Players.Length > 0 ? Players[0] : default; }
        }

        public BulletSimState[] Bullets { get; } // 当前世界中的所有子弹状态。

        public EnemySimState[] Enemies { get; }// 当前世界中的所有敌人状态。

        public WorldSnapshot(
            int tick,
            SimulationConfig config,
            PlayerSimState player,
            BulletSimState[] bullets,
            EnemySimState[] enemies)
            : this(tick, config, new PlayerSimState[] { player }, bullets, enemies)
        {
            
        }

        public WorldSnapshot(int tick, SimulationConfig config, PlayerSimState[] players, BulletSimState[] bullets, EnemySimState[] enemies)
        {
            Tick = tick;
            Config = config;
            Players = players == null ? new PlayerSimState[0] : (PlayerSimState[])players.Clone();
            Bullets = bullets == null ? new BulletSimState[0] : (BulletSimState[])bullets.Clone();
            Enemies = enemies == null ? new EnemySimState[0] : (EnemySimState[])enemies.Clone();
        }

        public string PlayersDetail()
        {
            if (Players == null || Players.Length == 0)
                return "No players.";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < Players.Length; i++)
            {
                var p = Players[i];
                sb.AppendLine($"Player {p.EntityId}: Pos={p.Position}, Aim={p.AimDirection}, HP={p.Hp}, Alive={p.IsAlive}, FireCD={p.FireCooldownTicks}, RespawnCD={p.RespawnCountdownTicks}, InvincibleTicks={p.InvincibleTicks}");
            }
            return sb.ToString();
        }
    }
}