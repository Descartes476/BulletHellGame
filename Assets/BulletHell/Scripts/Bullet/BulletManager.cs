using System.Collections.Generic;
using UnityEngine;
public enum BulletFaction
{
    Player = 0,
    Enemy = 1,
}

public class BulletManager : MonoBehaviour
{
    public static BulletManager Instance { get; private set; }

    

    [Header("Prefabs")]
    public GameObject playerBulletPrefab;
    public GameObject enemyBulletPrefab;

    private ObjectPool playerBulletPool;
    private ObjectPool enemyBulletPool;

    public int preWarmCount = 100;  // 预热数量
    private readonly List<Bullet> activePlayerBullets = new List<Bullet>();
    private readonly List<Bullet> activeEnemyBullets = new List<Bullet>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (playerBulletPrefab == null || enemyBulletPrefab == null)
        {
            Debug.LogError("BulletManager: playerBulletPrefab or enemyBulletPrefab is null");
            return;
        }

        Transform bulletPoolRoot = GameObject.Find("BulletPoolRoot")?.transform;
        if (bulletPoolRoot == null)
        {
            bulletPoolRoot = new GameObject("BulletPoolRoot").transform;
            bulletPoolRoot.SetParent(transform);
        }
        playerBulletPool = new ObjectPool(playerBulletPrefab, bulletPoolRoot, preWarmCount);
        enemyBulletPool = new ObjectPool(enemyBulletPrefab, bulletPoolRoot, preWarmCount);
    }

    // Update is called once per frame
    void Update()
    {
        float deltaTime = Time.deltaTime;
        for(int i = activePlayerBullets.Count - 1; i >= 0; i--)
        {
            var bullet = activePlayerBullets[i];
            bool alive = bullet.Tick(deltaTime);
            if (!alive)
            {
                activePlayerBullets.RemoveAt(i);
                playerBulletPool.Return(bullet.gameObject);
                continue;
            }

            if (TryHitEnemy(bullet))
            {
                activePlayerBullets.RemoveAt(i);
                playerBulletPool.Return(bullet.gameObject);
            }
        }
        for(int i = activeEnemyBullets.Count - 1; i >= 0; i--)
        {
            var bullet = activeEnemyBullets[i];
            bool alive = bullet.Tick(deltaTime);
            if (!alive)
            {
                activeEnemyBullets.RemoveAt(i);
                enemyBulletPool.Return(bullet.gameObject);
                continue;
            }

            if(TryHitPlayer(bullet))
            {
                activeEnemyBullets.RemoveAt(i);
                enemyBulletPool.Return(bullet.gameObject);
            }
        }
    }

    private static bool TryHitEnemy(Bullet bullet)
    {
        var enemies = EnemyBase.ActiveEnemies;
        if (enemies == null || enemies.Count == 0)
            return false;

        Vector3 bulletPos3 = bullet.transform.position;
        Vector2 bulletPos = new Vector2(bulletPos3.x, bulletPos3.y);
        float bulletR = bullet.radius;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled)
                continue;

            Vector3 enemyPos3 = enemy.transform.position;
            Vector2 enemyPos = new Vector2(enemyPos3.x, enemyPos3.y);

            float r = bulletR + enemy.HitRadius;
            Vector2 d = enemyPos - bulletPos;
            if (d.sqrMagnitude <= r * r)
            {
                enemy.TakeDamage(bullet.damage);
                return true;
            }
        }

        return false;
    }

    private static bool TryHitPlayer(Bullet bullet)
    {
        var players = PlayerBase.ActivePlayers;
        if (players == null || players.Count == 0)
            return false;

        Vector3 bulletPos3 = bullet.transform.position;
        Vector2 bulletPos = new Vector2(bulletPos3.x, bulletPos3.y);
        float bulletR = bullet.radius;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player == null || !player.isActiveAndEnabled)
                continue;

            Vector3 playerPos3 = player.transform.position;
            Vector2 playerPos = new Vector2(playerPos3.x, playerPos3.y);

            float r = bulletR + player.HitRadius;
            Vector2 d = playerPos - bulletPos;
            if (d.sqrMagnitude <= r * r)
            {
                player.TakeDamage(bullet.damage);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 生成一颗敌方子弹
    /// </summary>
    /// <param name="position">生成位置（Vector2）</param>
    /// <param name="direction">发射方向（建议传入单位向量；若非单位向量，内部应自行归一化）</param>
    /// <param name="speed">子弹速度</param>
    /// <param name="damage">子弹伤害</param>
    /// <param name="lifetime">存活时间（秒），到期后回收/销毁</param>
    /// <param name="faction">子弹阵营（玩家/敌人）</param>
    public Bullet SpawnBullet(Vector3 position, Vector2 direction, float speed, float damage, float lifetime, BulletFaction faction)
    {
        if(faction == BulletFaction.Player)
        {
            GameObject bulletObj = playerBulletPool.Get();
            bulletObj.transform.position = position;
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet == null)
            {
                Debug.LogError("BulletManager: player bullet prefab missing Bullet component");
                playerBulletPool.Return(bulletObj);
                return null;
            }
            bullet.Init(position, direction, speed, damage, lifetime);
            activePlayerBullets.Add(bullet);
            return bullet;
        }
        else
        {
            GameObject bulletObj = enemyBulletPool.Get();
            bulletObj.transform.position = position;
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet == null)
            {
                Debug.LogError("BulletManager: enemy bullet prefab missing Bullet component");
                enemyBulletPool.Return(bulletObj);
                return null;
            }
            bullet.Init(position, direction, speed, damage, lifetime);
            activeEnemyBullets.Add(bullet);
            return bullet;
        }
    }

    public void RecycleBullet(Bullet bullet)
    {
        if (bullet == null)
            return;

        if (activePlayerBullets.Contains(bullet))
        {
            activePlayerBullets.Remove(bullet);
            playerBulletPool.Return(bullet.gameObject);
        }
        else if (activeEnemyBullets.Contains(bullet))
        {
            activeEnemyBullets.Remove(bullet);
            enemyBulletPool.Return(bullet.gameObject);
        }
    }


}
