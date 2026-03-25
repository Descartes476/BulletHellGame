using System.Collections.Generic;
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [SerializeField] private float fireInterval = 1f; //最短射击间隔
    [SerializeField] private float bulletSpeed = 2f;
    [SerializeField] private float bulletLifeTime = 30f;
    [SerializeField] private float bulletDamage = 1f;
    [SerializeField] private List<EnemyShootPattern> shootPatterns; // 可用的射击模式
    [SerializeField] private float aimJitterDegrees = 12f;
    [SerializeField] private int switchEveryShoot = 10; // 每射击几次切换模式
    private int _shotCount = 0;
    private EnemyShootPattern _currentPattern;


    

    private float _fireTimer;
    private Transform _player;
    private float _nextFindPlayerTime;

    public float BulletSpeed => bulletSpeed;
    public float BulletLifeTime => bulletLifeTime;
    public float BulletDamage => bulletDamage;
    public float AimJitterDegrees => aimJitterDegrees;

    // Start is called before the first frame update
    void Start()
    {
        _fireTimer = fireInterval;
        _nextFindPlayerTime = 0f;
        _player = GameObject.FindWithTag("Player")?.transform;
        
        // 如果没有在 Inspector 中分配射击模式，创建一个默认的（仅运行时使用，不会落盘）
        if (shootPatterns == null || shootPatterns.Count == 0)
        {
            var runtimeDefault = ScriptableObject.CreateInstance<RandomAimPattern>();
            runtimeDefault.hideFlags = HideFlags.HideAndDontSave;
            shootPatterns = new List<EnemyShootPattern> { runtimeDefault };
        }
    }

    // Update is called once per frame
    void Update()
    {
        

        if (_player == null)
        {
            // 避免每帧 Find，降低开销
            if (Time.unscaledTime < _nextFindPlayerTime)
            {
                return;
            }

            _nextFindPlayerTime = Time.unscaledTime + 1f;
            _player = GameObject.FindWithTag("Player")?.transform;
            if (_player == null)
            {
                Debug.LogWarning("EnemyShooter: No Player Found!");
                return;
            }
                
        }

        if (BulletManager.Instance == null)
            return;

        _fireTimer += Time.deltaTime;
        float interval = fireInterval <= 0f ? 0f : fireInterval;
        if (_fireTimer < interval)
            return;

        _fireTimer = 0f;

        // 如果没有当前模式或需要切换模式
        bool shouldSwitch = _currentPattern == null;
        if (!shouldSwitch && switchEveryShoot > 0 && _shotCount > 0)
            shouldSwitch = (_shotCount % switchEveryShoot) == 0;

        if (shouldSwitch)
        {
            if(shootPatterns != null && shootPatterns.Count > 0)
            {
                _currentPattern = SelectPatternByWeight();
            }
            else
            {
                Debug.LogWarning("EnemyShooter: No shoot patterns assigned!");
                return;
            }
        }
        
        _currentPattern.Shoot(this, transform.position, _player.position);
        _shotCount++;
    }

    // 根据权重随机选择射击模式
    private EnemyShootPattern SelectPatternByWeight()
    {
        float totalWeight = 0f;
        foreach (var pattern in shootPatterns)
        {
            if (pattern != null)
            {
                if (pattern.weight > 0f)
                    totalWeight += pattern.weight;
            }
        }

        if (totalWeight <= 0f)
        {
            foreach (var pattern in shootPatterns)
            {
                if (pattern != null)
                    return pattern;
            }
            return null;
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var pattern in shootPatterns)
        {
            if (pattern == null) continue;
            if (pattern.weight <= 0f) continue;
            
            currentWeight += pattern.weight;
            if (randomValue <= currentWeight)
            {
                return pattern;
            }
        }

        // 如果没有找到（理论上不应该发生），返回第一个非空模式
        foreach (var pattern in shootPatterns)
        {
            if (pattern != null)
                return pattern;
        }

        return null;
    }

    public void Shoot(Vector3 dir)
    {
        BulletManager.Instance.SpawnBullet(
            transform.position,
            dir,
            bulletSpeed,
            bulletDamage,
            bulletLifeTime,
            BulletFaction.Enemy
        );
    }
}
