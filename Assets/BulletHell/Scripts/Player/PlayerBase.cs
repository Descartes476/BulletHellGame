using System.Collections.Generic;
using UnityEngine;

public class PlayerBase : MonoBehaviour
{

    [SerializeField] private float hp = 100f;
    [SerializeField] private float hitRadius = 0.5f;
    private float _currentHp;
    private Vector3 _spawnPosition;
    private bool _hasSpawnPosition;
    private static readonly List<PlayerBase> activePlayersInternal = new List<PlayerBase>();   // 玩家注册表
    public static IReadOnlyList<PlayerBase> ActivePlayers => activePlayersInternal;
    public float HitRadius => hitRadius;
    public float CurrentHp => _currentHp;
    public float MaxHp => hp;
    public bool IsDead => _currentHp <= 0;
    public Vector3 SpawnPosition => _spawnPosition;

    // 玩家事件
    public static event System.Action<PlayerBase> OnPlayerDied;
    public static event System.Action<PlayerBase> OnPlayerSpawned;
    public static event System.Action<PlayerBase> OnPlayerHpChanged;


    public void TakeDamage(float damage)
    {
        if (damage <= 0f || IsDead)
            return;

        _currentHp -= damage;
        if (_currentHp < 0f)
            _currentHp = 0f;

        OnPlayerHpChanged?.Invoke(this);
        if(_currentHp <= 0f)
        {
            OnPlayerDied?.Invoke(this);
            gameObject.SetActive(false);
        }
    }

    public void Respawn()
    {
        transform.position = _spawnPosition;
        gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        activePlayersInternal.Remove(this);
    }

    private void Awake()
    {
        if (!_hasSpawnPosition)
        {
            _spawnPosition = transform.position;
            _hasSpawnPosition = true;
        }
    }

    private void OnEnable()
    {
        if (!_hasSpawnPosition)
        {
            _spawnPosition = transform.position;
            _hasSpawnPosition = true;
        }

        _currentHp = hp;
        if(!activePlayersInternal.Contains(this))
        {
            activePlayersInternal.Add(this);
        }
        OnPlayerSpawned?.Invoke(this);
    }

}
