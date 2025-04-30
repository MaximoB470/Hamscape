using UnityEngine;

using System.Collections.Generic;

public class UpdateManager : MonoBehaviour
{
    private readonly List<ICustomUpdate> updatables = new();

    private void Awake()
    {
        // Registrar el UpdateManager como servicio global
        ServiceLocator.Instance.Register<UpdateManager>(this);
    }

    public void Register(ICustomUpdate updatable)
    {
        if (!updatables.Contains(updatable))
            updatables.Add(updatable);
    }
    public void Unregister(ICustomUpdate updatable)
    {
        if (updatables.Contains(updatable))
            updatables.Remove(updatable);
    }
    private void Update()
    {
        foreach (var updatable in updatables)
        {
            updatable.CustomUpdate();
        }
    }

}