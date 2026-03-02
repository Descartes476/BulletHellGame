using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject enemyPrefab;
    [SerializeField]
    private int initialCount = 3;
    [SerializeField]
    private Vector2 spawnMin = new Vector2(-6f, 1f);
    [SerializeField]
    private Vector2 spawnMax = new Vector2(6f, 4f);

    // Start is called before the first frame update
    void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: enemyPrefab is null");
            return;
        }

        int count = Mathf.Max(0, initialCount);
        float minX = Mathf.Min(spawnMin.x, spawnMax.x);
        float maxX = Mathf.Max(spawnMin.x, spawnMax.x);
        float minY = Mathf.Min(spawnMin.y, spawnMax.y);
        float maxY = Mathf.Max(spawnMin.y, spawnMax.y);

        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(minX, maxX);
            float y = Random.Range(minY, maxY);
            Instantiate(enemyPrefab, new Vector3(x, y, 0f), Quaternion.identity, transform);
        }
    }
}
