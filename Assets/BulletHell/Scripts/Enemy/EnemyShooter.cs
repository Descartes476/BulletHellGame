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
