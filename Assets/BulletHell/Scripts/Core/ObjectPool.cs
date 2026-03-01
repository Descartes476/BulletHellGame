using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    // 用于创建对象的模板
    private readonly GameObject _prefab;
    // 池内对象默认挂载的父节点，便于层级管理
    private readonly Transform _parent;
    // 未被使用（inactive）的对象队列
    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    
    public ObjectPool(GameObject prefab, Transform parent, int initialCount = 0)
    {
        if (prefab == null)
            throw new System.ArgumentNullException(nameof(prefab));
        if (parent == null)
            throw new System.ArgumentNullException(nameof(parent));

        _prefab = prefab;
        _parent = parent;
        if (initialCount < 0)
            initialCount = 0;

        for (int i = 0; i < initialCount; i++)
        {
            GameObject obj = GameObject.Instantiate(_prefab, _parent);
            obj.SetActive(false);
            _pool.Enqueue(obj);
            
        }
    }

    // 当前池中可复用的（inactive）对象数量
    public int CountInactive => _pool.Count;
    
    public GameObject Get()
    {
        GameObject obj;
        if (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
            obj.transform.SetParent(_parent, false);
            obj.SetActive(true);
        }
        else
        {
            obj = GameObject.Instantiate(_prefab, _parent);
            obj.SetActive(true);
        }
        return obj;
    }
    
    public void Return(GameObject obj)
    {
        if (obj == null)
            return;

        obj.SetActive(false);
        obj.transform.SetParent(_parent, false);
        _pool.Enqueue(obj);
        
    }
}
