using System.Collections.Generic;
using UnityEngine;

public class PlayerBase : MonoBehaviour
{

    [SerializeField] private float hp = 10f;
    [SerializeField] private float hitRadius = 0.5f;
    private float _currentHp;
    private static readonly List<PlayerBase> activePlayersInternal = new List<PlayerBase>();
    public static IReadOnlyList<PlayerBase> ActivePlayers => activePlayersInternal;
    public float HitRadius => hitRadius;

    public void TakeDamage(float damage)
    {
        _currentHp -= damage;
        if(_currentHp <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        activePlayersInternal.Remove(this);
    }

    private void OnEnable()
    {
        _currentHp = hp;
        if(!activePlayersInternal.Contains(this))
        {
            activePlayersInternal.Add(this);
        }
    }

}
