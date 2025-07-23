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

                // Crear el componente
                _instance = go.AddComponent<UpdateManager>();

                // Crear y configurar AudioSource
                var audioSource = go.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;

                _instance.musicSource = audioSource;

                // Cargar el clip desde Resources
                _instance.musicClip = Resources.Load<AudioClip>("Assets/SceneMusic/NoMoreBugs"); 

                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private List<IStartable> _startables = new List<IStartable>();
    private List<IUpdatable> _updatables = new List<IUpdatable>();
    private bool _hasInitialized = false;

    // AGREGADO para el AudioController
    private AudioController audioController;
    private AudioSource musicSource;
    private AudioClip musicClip;

    private void Update()
    {
        if (!_hasInitialized)
        {
            InitializeAll();

            // INICIALIZAR MÚSICA
            if (musicSource != null && musicClip != null)
            {
                audioController = new AudioController(musicSource);
                audioController.PlayMusic(musicClip);
            }

            _hasInitialized = true;
        }

        for (int i = 0; i < _updatables.Count; i++)
        {
            _updatables[i].Tick(Time.deltaTime);
        }
    }

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
