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
    public float MaxHp => hp;
    public float CurrentHp => _currentHp;

    public static event System.Action OnEnemyDied;

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

}
