using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;



public class ObjectPool : MonoBehaviour, IStartable
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int poolSize = 10;

    private Queue<GameObject> pool = new Queue<GameObject>();

    protected virtual void Awake()
    {
        UpdateManager.Instance.RegisterStartable(this);
    }

    public virtual void Initialize()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public GameObject GetFromPool()
    {
        GameObject obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            obj = Instantiate(prefab);
        }

        obj.SetActive(true);
        return obj;
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }

    protected virtual void OnDestroy()
    {
        UpdateManager.Instance.UnregisterStartable(this);
    }
}