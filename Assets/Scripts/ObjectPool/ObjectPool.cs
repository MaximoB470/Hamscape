using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> : MonoBehaviour, IStartable where T : Component
{
    [SerializeField] private T prefab;
    [SerializeField] private int poolSize = 10;
    private Queue<T> pool = new Queue<T>();
    private void Awake()
    {
        UpdateManager.Instance.RegisterStartable(this);
    }

    public virtual void Initialize()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            T obj = Instantiate(prefab);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }
    public T GetFromPool()
    {
        T obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            obj = Instantiate(prefab);
        }
        obj.gameObject.SetActive(true);
        return obj;
    }
    public void ReturnToPool(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
    private void OnDestroy()
    {
        UpdateManager.Instance.UnregisterStartable(this);
    }
}