using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [SerializeField] 
    private float hp = 10f;

    private float _currentHp;

    void OnEnable()
    {
        _currentHp = hp;
    }

    public void TakeDamage(float dmg)
    {
        _currentHp -= dmg;
        if (_currentHp <= 0f)
        {
            gameObject.SetActive(false);
        }
    }
}
