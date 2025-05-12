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
    private List<IStartable> _startables = new List<IStartable>();
    private List<IUpdatable> _updatables = new List<IUpdatable>();
    private bool _hasInitialized = false;
    
    // Este es el único Update() permitido en todo el proyecto
    private void Update()
    {
        // Si no se ha inicializado, ejecuta Initialize() en todos los objetos registrados
        if (!_hasInitialized)
        {
            InitializeAll();
            _hasInitialized = true;
        }

        for (int i = 0; i < _updatables.Count; i++)
        {
            _updatables[i].Tick(Time.deltaTime);
        }
    }

    // Inicializa todos los objetos registrados como IStartable
    private void InitializeAll()
    {
        for (int i = 0; i < _startables.Count; i++)
        {
            _startables[i].Initialize();
        }
    }

    public void RegisterStartable(IStartable startable)
    {
        if (!_startables.Contains(startable))
        {
            _startables.Add(startable);
        }

        if (_hasInitialized)
        {
            startable.Initialize();
        }
    }

    public void UnregisterStartable(IStartable startable)
    {
        if (_startables.Contains(startable))
        {
            _startables.Remove(startable);
        }
    }

    public void Register(IUpdatable updatable)
    {
        if (!_updatables.Contains(updatable))
        {
            _updatables.Add(updatable);
        }
    }

    public void Unregister(IUpdatable updatable)
    {
        if (_updatables.Contains(updatable))
        {
            _updatables.Remove(updatable);
        }
    }
}