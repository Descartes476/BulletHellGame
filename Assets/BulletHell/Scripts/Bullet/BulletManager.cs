using System.Collections.Generic;
using UnityEngine;
public enum BulletFaction
{
    Player = 0,
    Enemy = 1,
}

// 负责子弹对象池、逐帧更新，以及玩家/敌人两套子弹的命中检测与回收。
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
        // 逆序遍历，便于在命中或过期后直接从活动列表中移除并回收。
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

    private static bool TryHitPlayer(Bullet bullet)
    {
        var players = PlayerBase.ActivePlayers;
        if (players == null || players.Count == 0)
            return false;

        Vector3 bulletPos3 = bullet.transform.position;
        float bulletR = bullet.radius;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player == null || !player.isActiveAndEnabled)
                continue;

            Vector3 playerPos3 = player.transform.position;

            float r = bulletR + player.HitRadius;
            float dx = playerPos3.x - bulletPos3.x;
            float dy = playerPos3.y - bulletPos3.y;
            float sqrDistanceInPanel = dx * dx + dy * dy;
            if (sqrDistanceInPanel <= r * r)
            {
                player.TakeDamage(bullet.damage);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 从对应阵营的对象池取出子弹并注册到活动列表，后续由 Update 统一驱动移动、命中和回收。
    /// </summary>
    /// <param name="position">生成位置（Vector2）</param>
    /// <param name="direction">发射方向（建议传入单位向量；若非单位向量，内部应自行归一化）</param>
    /// <param name="speed">子弹速度</param>
    /// <param name="damage">子弹伤害</param>
    /// <param name="lifetime">存活时间（秒），到期后回收/销毁</param>
    /// <param name="faction">子弹阵营（玩家/敌人）</param>
    public Bullet SpawnBullet(Vector3 position, Vector3 direction, float speed, float damage, float lifetime, BulletFaction faction)
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
