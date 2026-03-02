using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [SerializeField] private float fireInterval = 0.5f;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float bulletLifeTime = 2.5f;
    [SerializeField] private float bulletDamage = 1f;
    [SerializeField] private float aimJitterDegrees = 12f;

    private float _fireTimer;
    private Transform _player;
    private float _nextFindPlayerTime;

    // Start is called before the first frame update
    void Start()
    {
        _fireTimer = fireInterval;
        _nextFindPlayerTime = 0f;
        _player = GameObject.FindWithTag("Player")?.transform;
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
                return;
        }

        if (BulletManager.Instance == null)
            return;

        _fireTimer += Time.deltaTime;
        float interval = fireInterval <= 0f ? 0f : fireInterval;
        if (_fireTimer < interval)
            return;

        _fireTimer = 0f;

        Vector2 toPlayer = (Vector2)(_player.position - transform.position);
        if (toPlayer.sqrMagnitude < 1e-6f)
            return;

        Vector2 baseDir = toPlayer.normalized;
        float jitter = Random.Range(-aimJitterDegrees, aimJitterDegrees);
        Vector2 direction = Rotate(baseDir, jitter);

        BulletManager.Instance.SpawnBullet(
            transform.position,
            direction,
            bulletSpeed,
            bulletDamage,
            bulletLifeTime,
            BulletFaction.Enemy
        );
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
}
