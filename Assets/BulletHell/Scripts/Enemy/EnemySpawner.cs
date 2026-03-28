using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject enemyPrefab;
    [SerializeField]
    private int initialCount = 3;
    [SerializeField]
    private int seed = 1;
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
        Fix64 minX = (Fix64)Mathf.Min(spawnMin.x, spawnMax.x);
        Fix64 maxX = (Fix64)Mathf.Max(spawnMin.x, spawnMax.x);
        Fix64 minY = (Fix64)Mathf.Min(spawnMin.y, spawnMax.y);
        Fix64 maxY = (Fix64)Mathf.Max(spawnMin.y, spawnMax.y);
        DeterministicRandom random = new DeterministicRandom(unchecked((uint)seed));

        for (int i = 0; i < count; i++)
        {
            Fix64 x = random.RangeFix(minX, maxX);
            Fix64 y = random.RangeFix(minY, maxY);
            Vector3 spawnPosition = new Vector3((float)x, (float)y, 0f);
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform);
        }
    }
}
