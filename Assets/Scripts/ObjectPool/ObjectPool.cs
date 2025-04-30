using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : MonoBehaviour
{
    private T _prefab;
    private List<T> _pool = new List<T>();
    private Transform _parent;

    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent;

        // Crear el pool inicial
        for (int i = 0; i < initialSize; i++)
        {
            CreateObject();
        }
    }

    private T CreateObject()
    {
        T obj = GameObject.Instantiate(_prefab, _parent);
        obj.gameObject.SetActive(false);
        _pool.Add(obj);
        return obj;
    }

    public T GetObject()
    {
        // Buscar un objeto inactivo
        foreach (T obj in _pool)
        {
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.gameObject.SetActive(true);
                return obj;
            }
        }

        // Si no hay objetos inactivos, crear uno nuevo
        return CreateObject();
    }

    public void ReturnObject(T obj)
    {
        obj.gameObject.SetActive(false);
    }
}