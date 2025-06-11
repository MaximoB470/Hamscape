using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AssetsManager : MonoBehaviour
{
    public static AssetsManager Instance { get; private set; }

    [SerializeField]
    private List<AssetReference> assetReferences;

    private Dictionary<string, GameObject> loadedAssets;
    private Dictionary<string, AsyncOperationHandle<GameObject>> assetHandles;

    public event Action OnLoadComplete;
    public event Action<float> OnLoadProgress;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        loadedAssets = new Dictionary<string, GameObject>();
        assetHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

        LoadAssets();
    }

    private void LoadAssets()
    {
        StartCoroutine(LoadAssetsCoroutine());
    }

    private IEnumerator LoadAssetsCoroutine()
    {
        int assetsToLoad = assetReferences.Count;
        int assetsLoaded = 0;

        Debug.Log($"Starting to load {assetsToLoad} assets...");

        foreach (AssetReference assetReference in assetReferences)
        {
            // Cargar asset de forma asíncrona
            AsyncOperationHandle<GameObject> handle = assetReference.LoadAssetAsync<GameObject>();
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // Extraer nombre del asset (primera palabra antes del espacio)
                string assetName = handle.Result.name.Split(' ')[0];

                // Evitar duplicados
                if (!loadedAssets.ContainsKey(assetName))
                {
                    loadedAssets.Add(assetName, handle.Result);
                    assetHandles.Add(assetName, handle);

                    Debug.Log($"Successfully loaded asset: {assetName}");
                }
                else
                {
                    Debug.LogWarning($"Asset with name '{assetName}' already exists. Skipping duplicate.");
                    Addressables.Release(handle);
                }

                assetsLoaded++;
            }
            else
            {
                Debug.LogError($"Failed to load asset: {assetReference}. Status: {handle.Status}");

                // Liberar handle fallido
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            // Notificar progreso de carga
            float progress = (float)assetsLoaded / assetsToLoad;
            OnLoadProgress?.Invoke(progress);
        }

        // Notificar completación si se cargó al menos un asset
        if (assetsLoaded > 0)
        {
            Debug.Log($"Asset loading complete! Loaded {assetsLoaded}/{assetsToLoad} assets.");
            OnLoadComplete?.Invoke();
        }
        else
        {
            Debug.LogError("No assets were loaded successfully!");
        }
    }

    public void SubscribeOnLoadComplete(Action callback)
    {
        OnLoadComplete += callback;
    }

    public void SubscribeOnLoadProgress(Action<float> callback)
    {
        OnLoadProgress += callback;
    }

    public GameObject GetInstance(string assetName)
    {
        if (loadedAssets.ContainsKey(assetName))
        {
            return Instantiate(loadedAssets[assetName]);
        }

        Debug.LogError($"Asset '{assetName}' not found in loaded assets. Available assets: {string.Join(", ", loadedAssets.Keys)}");
        return null;
    }

    public bool IsAssetLoaded(string assetName)
    {
        return loadedAssets.ContainsKey(assetName);
    }

    public GameObject GetAssetReference(string assetName)
    {
        if (loadedAssets.ContainsKey(assetName))
        {
            return loadedAssets[assetName];
        }

        Debug.LogError($"Asset '{assetName}' not found in loaded assets.");
        return null;
    }

    public List<string> GetLoadedAssetNames()
    {
        return new List<string>(loadedAssets.Keys);
    }

    public int GetLoadedAssetsCount()
    {
        return loadedAssets.Count;
    }

    public void ReleaseAsset(string assetName)
    {
        if (assetHandles.ContainsKey(assetName))
        {
            Addressables.Release(assetHandles[assetName]);
            assetHandles.Remove(assetName);
            loadedAssets.Remove(assetName);

            Debug.Log($"Released asset: {assetName}");
        }
        else
        {
            Debug.LogWarning($"Asset '{assetName}' not found for release.");
        }
    }
    public void ReleaseAllAssets()
    {
        foreach (var handle in assetHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        assetHandles.Clear();
        loadedAssets.Clear();

        Debug.Log("All assets released from memory.");
    }

    private void OnDestroy()
    {
        ReleaseAllAssets();
    }
}