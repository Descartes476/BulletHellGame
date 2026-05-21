using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace BulletHell.Simulation.Core
{
    /// <summary>
    /// 视图同步管理器，负责将仿真状态同步到Unity视图层
    /// </summary>
    public class ViewSyncManager : MonoBehaviour
    {
        // 子弹实体ID到显示对象的映射
        private readonly Dictionary<int, Bullet> _bulletViews = new Dictionary<int, Bullet>();
        // 敌人实体ID到显示对象的映射
        private readonly Dictionary<int, EnemyBase> _enemyViews = new Dictionary<int, EnemyBase>();
        // 玩家实体ID到显示对象的映射
        private readonly Dictionary<int, PlayerController> _playerViews = new Dictionary<int, PlayerController>();
        private readonly List<PlayerController> playerControllers = new List<PlayerController>();

        private SimulationConfig _config;
        [Header("Player 2 Visual")]
        [SerializeField] private PlayerController _playerPrefab; // 用于实例化玩家2的可视化对象
        public static ViewSyncManager Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetConfig(SimulationConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// 初始化视图同步管理器
        /// </summary>
        public void ResetSceneView(int playerID, Dictionary<int, Vector3> initialPlayerPosition, List<EnemyBase> initialSceneEnemies)
        {
            ClearBulletViews();
            if(!_playerViews.TryGetValue(playerID, out var playerController))
            {
                Debug.LogError("ViewSyncManager: PlayerController was not found when switching to replay mode.");
                return;
            }
            initialPlayerPosition.TryGetValue(playerID, out Vector3 startPos);
            SetPlayerView(playerID, playerController, startPos);
        }

        public void RestoreSceneActors(
            Dictionary<int, Vector3> initialPlayerPosition,
            List<EnemyBase> initialSceneEnemies,
            Dictionary<EnemyBase, Vector3> initialEnemyPositions
        )
        {
            List<int> PlayersID = initialPlayerPosition.Keys.ToList();
            for(int i = 0; i < PlayersID.Count; i++)
            {
                int playerID = PlayersID[i];
                if(!_playerViews.TryGetValue(playerID, out PlayerController playerController))
                {
                    SetPlayerView(playerID);
                }
                initialPlayerPosition.TryGetValue(playerID, out Vector3 startPos);
                playerController.gameObject.SetActive(true);
                playerController.transform.position = startPos;
            }
            

            for (int i = 0; i < initialSceneEnemies.Count; i++)
            {
                EnemyBase enemy = initialSceneEnemies[i];
                if (enemy == null)
                {
                    continue;
                }

                if (initialEnemyPositions.TryGetValue(enemy, out Vector3 initialPosition))
                {
                    enemy.gameObject.SetActive(true);
                    enemy.transform.position = initialPosition;
                }
            }
        }

        /// <summary>
        /// 设置玩家视图映射
        /// </summary>
        public void SetPlayerView(int playerId, PlayerController playerController = null, Vector3 startPos = new Vector3())
        {
            if(playerController == null)
            {
                if (_playerPrefab != null)
                {
                    playerController = Instantiate(_playerPrefab, startPos, Quaternion.identity);
                }
                else
                {
                    Debug.LogError("ViewSynManager: Player Prefab is not assigned.");
                }
            }
            
            _playerViews[playerId] = playerController;
        }

        public PlayerController GetPlayerView(int playerID)
        {
            _playerViews.TryGetValue(playerID, out PlayerController playerController);
            return playerController;
        }

        public PlayerController[] GetAllPlayerView()
        {
            return _playerViews.Values.ToArray();
        }

        /// <summary>
        /// 设置敌人视图映射
        /// </summary>
        public void SetEnemyViews(Dictionary<int, EnemyBase> enemyViews)
        {
            _enemyViews.Clear();
            foreach (var kvp in enemyViews)
            {
                _enemyViews[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// 同步子弹视图
        /// </summary>
        public void SyncBulletViews(BulletSimState[] bullets)
        {
            if (BulletManager.Instance == null)
                return;

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
                        bullet.RemainingLifetimeTicks * (1.0f / _config.TickRate),
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

        /// <summary>
        /// 同步敌人视图
        /// </summary>
        public void SyncEnemyViews(EnemySimState[] enemies)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (!_enemyViews.TryGetValue(enemy.EntityId, out EnemyBase enemyView))
                {
                    continue;
                }
                if (enemyView == null)
                {
                    continue;
                }
                if (!enemy.IsAlive)
                {
                    if (enemyView.gameObject.activeSelf)
                    {
                        enemyView.gameObject.SetActive(false);
                    }
                    continue;
                }
                else
                {
                    if (!enemyView.gameObject.activeSelf)
                    {
                        enemyView.gameObject.SetActive(true);
                    }
                }
                enemyView.transform.position = enemy.Position.ToVector3();
            }
        }

        /// <summary>
        /// 同步玩家视图，返回需要触发的事件
        /// </summary>
        public PlayerViewSyncResult SyncPlayerView(PlayerSimState oldPlayerState, PlayerSimState newPlayerState, int playerId)
        {
            var result = new PlayerViewSyncResult();

            if (!_playerViews.TryGetValue(playerId, out PlayerController playerController))
            {
                Debug.LogError("ViewSyncManager: PlayerController was not found.");
                return result;
            }

            PlayerBase playerBase = playerController.GetComponent<PlayerBase>();
            if (!playerBase)
            {
                Debug.LogError("ViewSyncManager: PlayerBase was not found.");
                return result;
            }

            // 玩家死亡、复活事件触发
            if (!newPlayerState.IsAlive)
            {
                playerController.gameObject.SetActive(false);
                result.RespawnCountdownChanged = true;
                result.RespawnCountdownTicks = newPlayerState.RespawnCountdownTicks;
            }
            else
            {
                playerController.gameObject.SetActive(true);
                playerBase.UpdateInvincibleVisual(newPlayerState.IsInvincible);
                if (!oldPlayerState.IsAlive)
                {
                    result.RespawnCountdownChanged = true;
                    result.RespawnCountdownTicks = newPlayerState.RespawnCountdownTicks;
                    result.PlayerSpawned = true;
                    result.CurrentHp = (int)newPlayerState.Hp;
                    result.MaxHp = (int)_config.PlayerMaxHp;
                }
            }

            // HUD血量
            if (oldPlayerState.Hp != newPlayerState.Hp)
            {
                result.HpChanged = true;
                result.CurrentHp = (int)newPlayerState.Hp;
                result.MaxHp = (int)_config.PlayerMaxHp;
            }

            FixVector3 simulatedPosition = newPlayerState.Position;
            playerController.transform.position = simulatedPosition.ToVector3();

            return result;
        }

        /// <summary>
        /// 清理所有子弹视图
        /// </summary>
        public void ClearBulletViews()
        {
            if (BulletManager.Instance != null)
            {
                foreach (KeyValuePair<int, Bullet> pair in _bulletViews)
                {
                    if (pair.Value != null)
                    {
                        BulletManager.Instance.RecycleBullet(pair.Value);
                    }
                }
            }

            _bulletViews.Clear();
        }
    }

    /// <summary>
    /// 玩家视图同步结果，包含需要触发的事件信息
    /// </summary>
    public struct PlayerViewSyncResult
    {
        public bool HpChanged;
        public bool PlayerSpawned;
        public bool RespawnCountdownChanged;
        public int CurrentHp;
        public int MaxHp;
        public int RespawnCountdownTicks;
    }
}
