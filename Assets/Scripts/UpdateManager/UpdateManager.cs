using UnityEngine;

using System.Collections.Generic;

public class UpdateManager : MonoBehaviour
{
    private static UpdateManager _instance;
    public static UpdateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UpdateManager");
                _instance = go.AddComponent<UpdateManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Lista de objetos actualizables
    private List<IUpdatable> _updatables = new List<IUpdatable>();

    // Este es el único Update() permitido en todo el proyecto
    private void Update()
    {
        for (int i = 0; i < _updatables.Count; i++)
        {
            _updatables[i].Tick(Time.deltaTime);
        }
    }
    // Registro de objetos actualizables
    public void Register(IUpdatable updatable)
    {
        if (!_updatables.Contains(updatable))
        {
            _updatables.Add(updatable);
        }
    }
    // Eliminar registro de objetos actualizables
    public void Unregister(IUpdatable updatable)
    {
        if (_updatables.Contains(updatable))
        {
            _updatables.Remove(updatable);
        }
    }
}