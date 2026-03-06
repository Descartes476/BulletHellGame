using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [SerializeField] 
    private float hp = 10f;

    [SerializeField]
    private float hitRadius = 0.5f;

    private float _currentHp;

    private static readonly System.Collections.Generic.List<EnemyBase> ActiveEnemiesInternal = new System.Collections.Generic.List<EnemyBase>();
    public static System.Collections.Generic.IReadOnlyList<EnemyBase> ActiveEnemies => ActiveEnemiesInternal;

    public float HitRadius => hitRadius;

    public static event System.Action<EnemyBase> OnEnemyDied;

    void OnEnable()
    {
        _currentHp = hp;

        if (!ActiveEnemiesInternal.Contains(this))
            ActiveEnemiesInternal.Add(this);
    }

    void OnDisable()
    {
        ActiveEnemiesInternal.Remove(this);
    }

    public void TakeDamage(float dmg)
    {
        if (dmg <= 0f || _currentHp <= 0f)
            return;

        _currentHp -= dmg;
        if (_currentHp < 0f)
            _currentHp = 0f;

        if (_currentHp <= 0f)
        {
            OnEnemyDied?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
