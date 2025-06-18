using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class SceneLoaderManager : MonoBehaviour, IStartable, IUpdatable
{
    [System.Serializable]
    public class SceneData
    {
        public string sceneName;
        public AssetReference sceneReference;
        [Tooltip("Assets específicos que deben cargarse antes de esta escena")]
        public List<AssetReference> requiredAssets = new List<AssetReference>();
    }

    private static SceneLoaderManager _instance;
    public static SceneLoaderManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SceneLoaderManager");
                _instance = go.AddComponent<SceneLoaderManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Scene Configuration")]
    [SerializeField] private List<SceneData> _scenes = new List<SceneData>();
    [SerializeField] private string _loadingSceneName = "LoadingScene";

    [Header("Loading Screen UI References")]
    [SerializeField] private string _loadingProgressTextTag = "LoadingProgressText";
    [SerializeField] private string _loadingBarTag = "LoadingBar";
    [SerializeField] private string _loadingTipTextTag = "LoadingTipText";
    [SerializeField] private string _continuePromptTextTag = "ContinuePromptText";

    [Header("Input Settings")]
    [SerializeField] private KeyCode _continueKey = KeyCode.Space;
    [SerializeField] private bool _requireInputToContinue = true;
    [SerializeField] private float _minimumLoadingTime = 1f; // Tiempo mínimo en loading screen

    // Loading tips para mostrar durante la carga
    [Header("Loading Tips")]
    [SerializeField]
    private List<string> _loadingTips = new List<string>
    {
        "Tip: Usa WASD para moverte",
        "Tip: Presiona ESC para pausar",
        "Tip: Recoge power-ups para mejorar tus habilidades",
        "Tip: Los enemigos son más débiles por la espalda"
    };

    // Estado interno
    private bool _isLoading = false;
    private bool _loadingComplete = false;
    private bool _waitingForInput = false;
    private float _loadingStartTime = 0f;
    private string _targetSceneName;
    private AsyncOperationHandle<SceneInstance> _currentSceneHandle;
    private List<AsyncOperationHandle> _loadedAssetHandles = new List<AsyncOperationHandle>();

    // Referencias UI de la pantalla de carga
    private TextMeshProUGUI _progressText;
    private Slider _progressBar;
    private TextMeshProUGUI _tipText;
    private TextMeshProUGUI _continuePromptText;

    // Eventos
    public event Action<string> OnSceneLoadStarted;
    public event Action<string> OnSceneLoadCompleted;
    public event Action<float> OnLoadProgress;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            UpdateManager.Instance.RegisterStartable(this);
            UpdateManager.Instance.Register(this); // Registrar para Update
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        Debug.Log("SceneLoaderManager initialized");
    }

    public void Tick(float deltaTime)
    {
        // Solo procesar input cuando estamos esperando
        if (_waitingForInput)
        {
            if (Input.GetKeyDown(_continueKey) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ContinueToTargetScene();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si estamos en la escena de carga, buscar referencias UI
        if (scene.name == _loadingSceneName)
        {
            SetupLoadingScreenUI();
        }
    }

    private void SetupLoadingScreenUI()
    {
        // Buscar elementos UI de la pantalla de carga por tags
        _progressText = FindUIElementByTag<TextMeshProUGUI>(_loadingProgressTextTag);
        _progressBar = FindUIElementByTag<Slider>(_loadingBarTag);
        _tipText = FindUIElementByTag<TextMeshProUGUI>(_loadingTipTextTag);
        _continuePromptText = FindUIElementByTag<TextMeshProUGUI>(_continuePromptTextTag);

        // Mostrar tip aleatorio
        if (_tipText != null && _loadingTips.Count > 0)
        {
            string randomTip = _loadingTips[UnityEngine.Random.Range(0, _loadingTips.Count)];
            _tipText.text = randomTip;
        }

        // Ocultar prompt de continuar inicialmente
        if (_continuePromptText != null)
        {
            _continuePromptText.gameObject.SetActive(false);
        }

        // Inicializar progress
        UpdateLoadingProgress(0f, "Iniciando carga...");

        Debug.Log("=== UI LOADING SCREEN CONFIGURADA ===");
        Debug.Log($"Progress Text encontrado: {_progressText != null}");
        Debug.Log($"Progress Bar encontrado: {_progressBar != null}");
        Debug.Log($"Tip Text encontrado: {_tipText != null}");
        Debug.Log($"Continue Prompt encontrado: {_continuePromptText != null}");
    }

    private T FindUIElementByTag<T>(string tag) where T : Component
    {
        try
        {
            GameObject obj = GameObject.FindGameObjectWithTag(tag);
            return obj?.GetComponent<T>();
        }
        catch (UnityException)
        {
            Debug.LogWarning($"No se encontró elemento UI con tag: {tag}");
            return null;
        }
    }

    /// <summary>
    /// Carga una escena usando addressables con pantalla de carga
    /// </summary>
    /// <param name="sceneName">Nombre de la escena a cargar</param>
    public void LoadScene(string sceneName)
    {
        if (_isLoading)
        {
            Debug.LogWarning("Ya hay una carga en progreso, ignorando nueva solicitud");
            return;
        }

        // DEBUG: Mostrar escenas configuradas
        Debug.Log($"=== INTENTANDO CARGAR ESCENA: {sceneName} ===");
        Debug.Log($"Escenas configuradas: {_scenes.Count}");
        for (int i = 0; i < _scenes.Count; i++)
        {
            Debug.Log($"  [{i}] {_scenes[i].sceneName} - Valid: {(_scenes[i].sceneReference != null && _scenes[i].sceneReference.RuntimeKeyIsValid())}");
        }

        SceneData sceneData = _scenes.Find(s => s.sceneName == sceneName);
        if (sceneData == null)
        {
            Debug.LogError($"❌ No se encontró configuración para la escena: {sceneName}");
            Debug.LogError("Escenas disponibles:");
            foreach (var scene in _scenes)
            {
                Debug.LogError($"  - {scene.sceneName}");
            }
            return;
        }

        if (sceneData.sceneReference == null || !sceneData.sceneReference.RuntimeKeyIsValid())
        {
            Debug.LogError($"❌ AssetReference inválido para la escena: {sceneName}");
            Debug.LogError($"AssetReference null: {sceneData.sceneReference == null}");
            if (sceneData.sceneReference != null)
            {
                Debug.LogError($"RuntimeKey válido: {sceneData.sceneReference.RuntimeKeyIsValid()}");
            }
            return;
        }

        Debug.Log($"✅ Configuración válida encontrada para {sceneName}. Iniciando carga...");

        _targetSceneName = sceneName;
        _isLoading = true;
        _loadingComplete = false;
        _waitingForInput = false;
        _loadingStartTime = Time.time;

        OnSceneLoadStarted?.Invoke(sceneName);

        // Cargar primero la escena de loading
        Debug.Log($"Cargando LoadingScene: {_loadingSceneName}");
        SceneManager.LoadScene(_loadingSceneName);

        // Iniciar la carga de la escena objetivo después de un frame
        StartCoroutine(DelayedSceneLoad(sceneData));
    }

    private IEnumerator DelayedSceneLoad(SceneData sceneData)
    {
        // Esperar un frame para que se cargue completamente la loading screen
        yield return null;
        yield return null;

        // Iniciar la carga real de la escena
        yield return StartCoroutine(LoadSceneAsync(sceneData));
    }

    private IEnumerator LoadSceneAsync(SceneData sceneData)
    {
        float totalProgress = 0f;
        int totalSteps = 1 + sceneData.requiredAssets.Count;
        float stepProgress = 1f / totalSteps;
        bool hasError = false;

        Debug.Log($"=== INICIANDO CARGA ASÍNCRONA DE {sceneData.sceneName} ===");
        Debug.Log($"Total de pasos: {totalSteps}");
        Debug.Log($"Assets requeridos: {sceneData.requiredAssets.Count}");

        // Paso 1: Cargar assets requeridos
        if (sceneData.requiredAssets.Count > 0)
        {
            UpdateLoadingProgress(0f, "Cargando recursos...");
            Debug.Log("Cargando assets requeridos...");

            for (int i = 0; i < sceneData.requiredAssets.Count && !hasError; i++)
            {
                AssetReference assetRef = sceneData.requiredAssets[i];
                if (assetRef != null && assetRef.RuntimeKeyIsValid())
                {
                    Debug.Log($"Cargando asset {i + 1}/{sceneData.requiredAssets.Count}");
                    var assetHandle = assetRef.LoadAssetAsync<GameObject>();
                    _loadedAssetHandles.Add(assetHandle);

                    yield return assetHandle;

                    if (assetHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Debug.Log($"✅ Asset cargado: {assetHandle.Result.name}");
                    }
                    else
                    {
                        Debug.LogError($"❌ Error cargando asset: {assetRef}");
                        hasError = true;
                    }
                }

                totalProgress += stepProgress;
                UpdateLoadingProgress(totalProgress, "Cargando recursos...");
            }
        }

        // Paso 2: Cargar la escena principal
        if (!hasError)
        {
            Debug.Log("Iniciando carga de escena principal...");
            UpdateLoadingProgress(totalProgress, "Cargando escena...");

            _currentSceneHandle = sceneData.sceneReference.LoadSceneAsync(LoadSceneMode.Single, false); // false = no activar automáticamente

            // Monitorear progreso de carga de escena
            while (!_currentSceneHandle.IsDone)
            {
                float sceneProgress = _currentSceneHandle.PercentComplete;
                float currentStepProgress = totalProgress + (stepProgress * sceneProgress);

                UpdateLoadingProgress(currentStepProgress, "Cargando escena...");
                Debug.Log($"Progreso de carga: {currentStepProgress * 100:F1}%");
                yield return null;
            }

            if (_currentSceneHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"✅ Escena cargada exitosamente: {sceneData.sceneName}");

                // Esperar tiempo mínimo si es necesario
                float elapsedTime = Time.time - _loadingStartTime;
                if (elapsedTime < _minimumLoadingTime)
                {
                    float remainingTime = _minimumLoadingTime - elapsedTime;
                    UpdateLoadingProgress(1f, $"Carga completada! Esperando {remainingTime:F1}s...");
                    yield return new WaitForSeconds(remainingTime);
                }

                UpdateLoadingProgress(1f, "¡Carga completada!");
                _loadingComplete = true;

                // Decidir si esperar input o continuar automáticamente
                if (_requireInputToContinue)
                {
                    ShowContinuePrompt();
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                    ContinueToTargetScene();
                }
            }
            else
            {
                Debug.LogError($"❌ Error cargando escena: {sceneData.sceneName}. Status: {_currentSceneHandle.Status}");
                hasError = true;
            }
        }

        // Manejo de errores
        if (hasError)
        {
            Debug.LogError("Error durante la carga, volviendo al menú principal");
            UpdateLoadingProgress(0f, "Error en la carga");
            yield return new WaitForSeconds(2f);
            SceneManager.LoadScene("MenuScene");
            _isLoading = false;
        }
    }

    private void ShowContinuePrompt()
    {
        Debug.Log("✅ CARGA COMPLETADA - Esperando input del usuario");
        _waitingForInput = true;

        if (_continuePromptText != null)
        {
            _continuePromptText.gameObject.SetActive(true);
            _continuePromptText.text = $"Presiona {_continueKey} para continuar";
        }

        UpdateLoadingProgress(1f, "¡Listo! Presiona ESPACIO para continuar");
    }

    private void ContinueToTargetScene()
    {
        if (!_loadingComplete || !_waitingForInput) return;

        Debug.Log($"🚀 CONTINUANDO A ESCENA: {_targetSceneName}");
        _waitingForInput = false;

        if (_continuePromptText != null)
        {
            _continuePromptText.gameObject.SetActive(false);
        }

        // Activar la escena que ya está cargada
        if (_currentSceneHandle.IsValid())
        {
            _currentSceneHandle.Result.ActivateAsync();
        }

        OnSceneLoadCompleted?.Invoke(_targetSceneName);
        _isLoading = false;
    }

    private void UpdateLoadingProgress(float progress, string statusText)
    {
        // Actualizar UI
        if (_progressBar != null)
        {
            _progressBar.value = progress;
        }

        if (_progressText != null)
        {
            _progressText.text = $"{statusText} {Mathf.RoundToInt(progress * 100)}%";
        }

        // Log para debugging
        Debug.Log($"🔄 Loading Progress: {progress * 100:F1}% - {statusText}");

        // Notificar evento
        OnLoadProgress?.Invoke(progress);
    }

    /// <summary>
    /// Método de conveniencia para testing - permitir continuar por código
    /// </summary>
    [ContextMenu("Force Continue")]
    public void ForceContinue()
    {
        if (_waitingForInput)
        {
            ContinueToTargetScene();
        }
    }

    /// <summary>
    /// Verificar configuración desde el inspector
    /// </summary>
    [ContextMenu("Verificar Configuración")]
    public void VerifyConfiguration()
    {
        Debug.Log("=== VERIFICACIÓN DE CONFIGURACIÓN ===");
        Debug.Log($"Loading Scene Name: {_loadingSceneName}");
        Debug.Log($"Continue Key: {_continueKey}");
        Debug.Log($"Require Input: {_requireInputToContinue}");
        Debug.Log($"Minimum Loading Time: {_minimumLoadingTime}s");
        Debug.Log($"Escenas configuradas: {_scenes.Count}");

        for (int i = 0; i < _scenes.Count; i++)
        {
            SceneData scene = _scenes[i];
            Debug.Log($"Escena [{i}]: {scene.sceneName}");

            if (scene.sceneReference == null)
            {
                Debug.LogError($"  ❌ SceneReference es NULL");
            }
            else
            {
                Debug.Log($"  AssetGUID: {scene.sceneReference.AssetGUID}");
                Debug.Log($"  RuntimeKey válido: {scene.sceneReference.RuntimeKeyIsValid()}");

                if (scene.sceneReference.RuntimeKeyIsValid())
                {
                    Debug.Log($"  ✅ Configuración válida");
                }
                else
                {
                    Debug.LogError($"  ❌ RuntimeKey inválido - verifica que la escena esté marcada como Addressable");
                }
            }

            Debug.Log($"  Required Assets: {scene.requiredAssets.Count}");
        }
    }

    /// <summary>
    /// Verifica si una escena está configurada
    /// </summary>
    public bool IsSceneConfigured(string sceneName)
    {
        return _scenes.Find(s => s.sceneName == sceneName) != null;
    }

    /// <summary>
    /// Obtiene la configuración de una escena
    /// </summary>
    public SceneData GetSceneData(string sceneName)
    {
        return _scenes.Find(s => s.sceneName == sceneName);
    }

    /// <summary>
    /// Método de conveniencia para usar desde UIManager
    /// </summary>
    public void LoadSceneWithLoading(string sceneName)
    {
        LoadScene(sceneName);
    }

    /// <summary>
    /// Liberar escena actual y sus assets
    /// </summary>
    public void UnloadCurrentScene()
    {
        if (_currentSceneHandle.IsValid())
        {
            Addressables.UnloadSceneAsync(_currentSceneHandle);
        }

        // Liberar assets cargados
        foreach (var handle in _loadedAssetHandles)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        _loadedAssetHandles.Clear();
    }

    public bool IsLoading => _isLoading;
    public bool IsWaitingForInput => _waitingForInput;
    public string CurrentTargetScene => _targetSceneName;

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnloadCurrentScene();

        if (UpdateManager.Instance != null)
        {
            UpdateManager.Instance.UnregisterStartable(this);
            UpdateManager.Instance.Unregister(this);
        }
    }

    // Métodos para integrar con UIManager
    #region UIManager Integration

    /// <summary>
    /// Método estático para fácil acceso desde UIManager
    /// </summary>
    public static void LoadSceneStatic(string sceneName)
    {
        Instance.LoadScene(sceneName);
    }

    #endregion
}