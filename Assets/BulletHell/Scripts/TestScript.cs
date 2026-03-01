using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public GameObject prefabBullet;
    public ObjectPool BulletPool;
    private readonly List<GameObject> activeBullets = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        if(prefabBullet == null)
        {
            Debug.LogError("prfabBullet is null");
            return;
        }
        BulletPool = new ObjectPool(prefabBullet, transform, 10);
        for(int i = 0; i < 20; i++)
        {
            print("创建Bullet"+i);
            GameObject bullet = BulletPool.Get();
            bullet.transform.position = new Vector3(i, 0, 0);
            bullet.SetActive(true);
            activeBullets.Add(bullet);
        }
        for(int i = 0; i < activeBullets.Count; i++)
        {
            BulletPool.Return(activeBullets[i]);   
        }
        activeBullets.Clear();
    }

    private void OnDestroy()
    {
        activeBullets.Clear();
    }
   
}
