using System;
using System.Collections.Generic;
using UnityEngine;
public class ServiceLocator
{
    private static ServiceLocator _instance;
    public static ServiceLocator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ServiceLocator();
            }
            return _instance;
        }
    }
    private Dictionary<Type, object> services = new Dictionary<Type, object>();

    private ServiceLocator() { }

    public void Register<T>(T service)
    {
        services[typeof(T)] = service;
    }
    public T GetService<T>()
    {
        if (services.TryGetValue(typeof(T), out object service))
        {
            return (T)service;
        }
        return default;
    }
    public bool IsServiceRegistered<T>()
    {
        return services.ContainsKey(typeof(T));
    }
    public void Unregister<T>()
    {
        if (services.ContainsKey(typeof(T)))
        {
            services.Remove(typeof(T));
        }
    }
}