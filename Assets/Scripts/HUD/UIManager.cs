using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class UIManager : MonoBehaviour, IStartable, IUpdatable
{
    [Header("Canvas Management")]
    [SerializeField] private List<CanvasInfo> _canvasInfos = new List<CanvasInfo>();

    [Header("UI Pooling")]
    [SerializeField] private UIPool _damageTextPool;
    [SerializeField] private UIPool _popupPool;

    [Header("Scene Canvas Configuration")]
    [SerializeField] private List<SceneCanvasConfig> _sceneCanvasConfigs = new List<SceneCanvasConfig>();
    
    [Header("Level Management")]
    [Tooltip("Nombre addressable del próximo nivel (configurado en SceneLoaderManager)")]
    [SerializeField] private string _nextLevelName;
    // Referencias dinámicas optimizadas
    private Dictionary<string, Button> _buttons = new Dictionary<string, Button>();
    private Dictionary<string, TextMeshProUGUI> _texts = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, GameObject> _panels = new Dictionary<string, GameObject>();
    private Dictionary<string, Slider> _sliders = new Dictionary<string, Slider>();
    private Dictionary<string, Canvas> _canvases = new Dictionary<string, Canvas>();
    private Dictionary<GameObject, Coroutine> _activeAnimations = new Dictionary<GameObject, Coroutine>();

    // Cache de componentes para evitar GetComponent repetitivos
    private Dictionary<Canvas, GraphicRaycaster> _canvasRaycastersCache = new Dictionary<Canvas, GraphicRaycaster>();
    private Dictionary<Canvas, bool> _canvasInteractiveCache = new Dictionary<Canvas, bool>();

    private HealthSystem _healthSystem;
    private bool _isPaused = false;
    private bool _hasInitialized = false;

    // Configuración actual de la escena
    private SceneCanvasConfig _currentSceneConfig;

    [System.Serializable]
    public class SceneCanvasConfig
    {
        public string sceneName;
        public string damageTextCanvasName = "GameCanvas";
        public string popupCanvasName = "UICanvas";
        public List<string> staticCanvases = new List<string>();
        public List<string> dynamicCanvases = new List<string>();
    }

    [System.Serializable]
    public class CanvasInfo
    {
        public string canvasName;
        public bool isStatic;
        public bool startActive;
        [Tooltip("Si es dinámico, se oculta cuando no se usa")]
        public bool hideWhenInactive;
        [Tooltip("Desactivar raycaster automáticamente si no tiene elementos interactivos")]
        public bool autoDisableRaycaster = true;
    }

    [System.Serializable]
    public class UIPool
    {
        public string poolName;
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 50;
        public Transform parentTransform;

        [HideInInspector] public Queue<GameObject> pool = new Queue<GameObject>();
        [HideInInspector] public List<GameObject> activeObjects = new List<GameObject>();
    }

    private void Awake()
    {
        // No es singleton, simplemente se registra en el UpdateManager y ServiceLocator
        UpdateManager.Instance.RegisterStartable(this);
        ServiceLocator.Instance.Register<UIManager>(this);
        UpdateManager.Instance.Register(this);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void Initialize()
    {
        _hasInitialized = true;
        SetupCurrentScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CleanupActiveAnimations();
        if (_hasInitialized)
        {
            SetupCurrentScene();
        }
    }

    private void SetupCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // Buscar configuración específica de la escena
        _currentSceneConfig = _sceneCanvasConfigs.Find(config => config.sceneName == currentSceneName);

        ClearReferences();
        ResetGameStateCanvases(); // NUEVO: Resetear canvas de estado de juego
        FindUIElements();
        SetupCanvasOptimization();
        UpdatePoolParentTransforms();
        InitializeUIPools();
        SetupButtonListeners();
        SetupHealthSystem();
    }

    #region Game State Canvas Reset
    private void ResetGameStateCanvases()
    {
        // Lista de canvas que necesitan estar disponibles pero ocultos al inicio
        string[] gameStateCanvases = { "VictoryCanvas", "DefeatCanvas" };

        foreach (string canvasName in gameStateCanvases)
        {
            // Buscar primero en todas las escenas (incluyendo inactivos)
            Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
            Canvas targetCanvas = System.Array.Find(allCanvases, c => c.name == canvasName);

            if (targetCanvas == null)
            {
                // Fallback: buscar por GameObject
                GameObject canvasObj = GameObject.Find(canvasName);
                if (canvasObj != null)
                {
                    targetCanvas = canvasObj.GetComponent<Canvas>();
                }
            }

            if (targetCanvas != null)
            {
                // Activar temporalmente para que sea detectado por el sistema
                targetCanvas.gameObject.SetActive(true);

                // Asegurar que tiene raycaster habilitado temporalmente
                GraphicRaycaster raycaster = targetCanvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = true;
                }

                // Registrar en el cache ANTES de ocultar
                _canvases[canvasName] = targetCanvas;

                // Ocultar inmediatamente después de registrar
                targetCanvas.gameObject.SetActive(false);

                Debug.Log($"Canvas {canvasName} registrado correctamente en el diccionario");
            }
        }
    }
    #endregion

    #region Canvas Optimization
    private void SetupCanvasOptimization()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();

        foreach (Canvas canvas in allCanvases)
        {
            string canvasName = canvas.gameObject.name;
            _canvases[canvasName] = canvas;

            // Obtener configuración del canvas
            CanvasInfo info = GetCanvasInfo(canvasName);
            if (info != null)
            {
                OptimizeCanvas(canvas, info);
            }
        }
    }

    private CanvasInfo GetCanvasInfo(string canvasName)
    {
        // Buscar configuración específica
        CanvasInfo info = _canvasInfos.Find(c => c.canvasName == canvasName);
        if (info != null) return info;

        // Usar configuración de escena si está disponible
        if (_currentSceneConfig != null)
        {
            bool isStatic = _currentSceneConfig.staticCanvases.Contains(canvasName);
            bool isDynamic = _currentSceneConfig.dynamicCanvases.Contains(canvasName);

            if (isStatic || isDynamic)
            {
                return new CanvasInfo
                {
                    canvasName = canvasName,
                    isStatic = isStatic,
                    startActive = true,
                    hideWhenInactive = isDynamic,
                    autoDisableRaycaster = true
                };
            }
        }

        // Configuración automática basada en nombres comunes
        return CreateAutoCanvasInfo(canvasName);
    }

    private CanvasInfo CreateAutoCanvasInfo(string canvasName)
    {
        string lowerName = canvasName.ToLower();

        bool isStatic = lowerName.Contains("static") ||
                       lowerName.Contains("background") ||
                       lowerName.Contains("hud");

        bool hideWhenInactive = lowerName.Contains("popup") ||
                               lowerName.Contains("dialog") ||
                               lowerName.Contains("pause") ||
                               lowerName.Contains("victory") ||  // NUEVO
                               lowerName.Contains("defeat");    // NUEVO

        return new CanvasInfo
        {
            canvasName = canvasName,
            isStatic = isStatic,
            startActive = !hideWhenInactive,
            hideWhenInactive = hideWhenInactive,
            autoDisableRaycaster = true
        };
    }

    private void OptimizeCanvas(Canvas canvas, CanvasInfo info)
    {
        if (info.isStatic)
        {
            // Optimizaciones para canvas estáticos
            canvas.overrideSorting = false;
            canvas.pixelPerfect = true;

            // Cachear información de interactividad
            bool hasInteractiveElements = CacheCanvasInteractivity(canvas);

            // Desactivar raycaster si no tiene elementos interactivos
            if (info.autoDisableRaycaster && !hasInteractiveElements)
            {
                SetCanvasRaycasterEnabled(canvas, false);
            }
        }
        else
        {
            // Optimizaciones para canvas dinámicos
            canvas.overrideSorting = true;
            canvas.pixelPerfect = false;
        }

        // Configurar estado inicial
        canvas.gameObject.SetActive(info.startActive);

        if (info.hideWhenInactive && !info.startActive)
        {
            HideCanvas(info.canvasName);
        }
    }

    private bool CacheCanvasInteractivity(Canvas canvas)
    {
        if (_canvasInteractiveCache.ContainsKey(canvas))
        {
            return _canvasInteractiveCache[canvas];
        }

        // Verificar elementos interactivos de manera más eficiente
        bool hasInteractive = canvas.GetComponentsInChildren<Selectable>().Length > 0;
        _canvasInteractiveCache[canvas] = hasInteractive;

        return hasInteractive;
    }

    private void SetCanvasRaycasterEnabled(Canvas canvas, bool enabled)
    {
        GraphicRaycaster raycaster = GetCachedRaycaster(canvas);
        if (raycaster != null)
        {
            raycaster.enabled = enabled;
        }
    }

    private GraphicRaycaster GetCachedRaycaster(Canvas canvas)
    {
        if (_canvasRaycastersCache.ContainsKey(canvas))
        {
            return _canvasRaycastersCache[canvas];
        }

        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            _canvasRaycastersCache[canvas] = raycaster;
        }

        return raycaster;
    }

    public void ShowCanvas(string canvasName)
    {
        if (_canvases.TryGetValue(canvasName, out Canvas canvas))
        {
            canvas.gameObject.SetActive(true);
            SetCanvasRaycasterEnabled(canvas, true);
            Debug.Log($"Mostrando canvas: {canvasName} con raycaster habilitado");
        }
        else
        {
            Debug.LogWarning($"Canvas {canvasName} no encontrado en el diccionario");
        }
    }

    public void HideCanvas(string canvasName)
    {
        if (_canvases.TryGetValue(canvasName, out Canvas canvas))
        {
            canvas.gameObject.SetActive(false);
            SetCanvasRaycasterEnabled(canvas, false);
            Debug.Log($"Ocultando canvas: {canvasName} con raycaster deshabilitado");
        }
    }

    public void ShowVictoryCanvas()
    {
        Time.timeScale = 0f;
        ShowCanvas("VictoryCanvas");
        EnableUIControls(true);
        Debug.Log("Victory canvas mostrado");
    }

    public void ShowDefeatCanvas()
    {
        Time.timeScale = 0f;

        // Verificar que el canvas esté en el diccionario antes de intentar mostrarlo
        if (!_canvases.ContainsKey("DefeatCanvas"))
        {
            Debug.LogError("DefeatCanvas no está registrado. Intentando re-registro...");
            ResetGameStateCanvases(); // Re-intentar registro
        }

        ShowCanvas("DefeatCanvas");

        // Asegurar que el raycaster esté habilitado específicamente para DefeatCanvas
        if (_canvases.TryGetValue("DefeatCanvas", out Canvas canvas))
        {
            SetCanvasRaycasterEnabled(canvas, true);
            Debug.Log("DefeatCanvas raycaster habilitado explícitamente");
        }

        RegisterRetryButton();
        EnableUIControls(true);
        Debug.Log("Defeat canvas mostrado y configurado");
    }

    private void RegisterRetryButton()
    {
        // Buscar el botón aunque no estuviera activo al inicio
        GameObject retryObj = GameObject.FindGameObjectWithTag("RetryButton");
        if (retryObj != null && retryObj.TryGetComponent(out Button button))
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(RetryLevel);
            Debug.Log("RetryButton registrado dinámicamente");
        }
        else
        {
            Debug.LogError("RetryButton no encontrado!");
        }
    }

    private void EnableUIControls(bool enable)
    {
        foreach (var canvas in _canvases.Values)
        {
            SetCanvasRaycasterEnabled(canvas, enable);
        }
    }

    #endregion

    #region Pool Management
    private void UpdatePoolParentTransforms()
    {
        if (_currentSceneConfig != null)
        {
            UpdatePoolParentTransform(_damageTextPool, _currentSceneConfig.damageTextCanvasName);
            UpdatePoolParentTransform(_popupPool, _currentSceneConfig.popupCanvasName);
        }
        else
        {
            UpdatePoolParentTransformAuto(_damageTextPool, "damage");
            UpdatePoolParentTransformAuto(_popupPool, "popup");
        }
    }

    private void UpdatePoolParentTransform(UIPool pool, string canvasName)
    {
        if (pool?.prefab == null) return;

        Canvas targetCanvas = FindCanvasByName(canvasName);
        if (targetCanvas != null)
        {
            pool.parentTransform = targetCanvas.transform;
            ReassignPoolObjectsToNewParent(pool);
        }
        else
        {
            Debug.LogWarning($"Canvas '{canvasName}' no encontrado para pool '{pool.poolName}'");
            UpdatePoolParentTransformAuto(pool, pool.poolName);
        }
    }

    private void UpdatePoolParentTransformAuto(UIPool pool, string poolType)
    {
        if (pool?.prefab == null) return;

        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas targetCanvas = null;

        string lowerPoolType = poolType.ToLower();

        if (lowerPoolType.Contains("damage"))
        {
            targetCanvas = System.Array.Find(canvases, c =>
                c.name.ToLower().Contains("game") ||
                c.name.ToLower().Contains("hud") ||
                c.name.ToLower().Contains("world") ||
                c.renderMode == RenderMode.WorldSpace);
        }
        else if (lowerPoolType.Contains("popup"))
        {
            targetCanvas = System.Array.Find(canvases, c =>
                c.name.ToLower().Contains("ui") ||
                c.name.ToLower().Contains("menu") ||
                c.renderMode == RenderMode.ScreenSpaceOverlay);
        }

        // Fallback: usar el primer canvas activo
        if (targetCanvas == null)
        {
            targetCanvas = System.Array.Find(canvases, c => c.gameObject.activeSelf);
        }

        if (targetCanvas != null)
        {
            pool.parentTransform = targetCanvas.transform;
            ReassignPoolObjectsToNewParent(pool);
        }
        else
        {
            Debug.LogError($"No se encontró canvas para el pool '{pool.poolName}'");
        }
    }

    private Canvas FindCanvasByName(string canvasName)
    {
        // Buscar en cache primero
        if (_canvases.TryGetValue(canvasName, out Canvas cachedCanvas) &&
            cachedCanvas != null)
        {
            return cachedCanvas;
        }

        // Buscar por nombre exacto
        GameObject canvasObj = GameObject.Find(canvasName);
        if (canvasObj != null)
        {
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas != null)
            {
                _canvases[canvasName] = canvas;
                return canvas;
            }
        }

        return null;
    }

    private void ReassignPoolObjectsToNewParent(UIPool pool)
    {
        if (pool.parentTransform == null) return;

        // Reasignar objetos en cola
        var pooledObjects = pool.pool.ToArray();
        foreach (GameObject obj in pooledObjects)
        {
            if (obj != null)
            {
                obj.transform.SetParent(pool.parentTransform, false);
            }
        }

        // Reasignar objetos activos
        foreach (GameObject obj in pool.activeObjects)
        {
            if (obj != null)
            {
                obj.transform.SetParent(pool.parentTransform, false);
            }
        }
    }

    private void InitializeUIPools()
    {
        if (_damageTextPool.prefab != null)
        {
            InitializePool(_damageTextPool);
        }

        if (_popupPool.prefab != null)
        {
            InitializePool(_popupPool);
        }
    }

    private void InitializePool(UIPool pool)
    {
        pool.pool ??= new Queue<GameObject>();
        pool.activeObjects ??= new List<GameObject>();

        if (pool.parentTransform == null)
        {
            GameObject parent = new GameObject(pool.poolName + "_Parent");
            pool.parentTransform = parent.transform;
        }

        int currentPoolSize = pool.pool.Count + pool.activeObjects.Count;
        int objectsToCreate = Mathf.Max(0, pool.initialSize - currentPoolSize);

        for (int i = 0; i < objectsToCreate; i++)
        {
            GameObject obj = Instantiate(pool.prefab, pool.parentTransform);
            obj.SetActive(false);
            pool.pool.Enqueue(obj);
        }
    }

    public GameObject GetPooledObject(string poolName)
    {
        UIPool pool = GetPool(poolName);
        if (pool == null) return null;

        GameObject obj = null;

        if (pool.pool.Count > 0)
        {
            obj = pool.pool.Dequeue();
        }
        else if (pool.activeObjects.Count < pool.maxSize)
        {
            obj = Instantiate(pool.prefab, pool.parentTransform);
        }
        else
        {
            // Reciclar el objeto más antiguo
            obj = pool.activeObjects[0];
            pool.activeObjects.RemoveAt(0);
        }

        if (obj != null)
        {
            obj.SetActive(true);
            pool.activeObjects.Add(obj);
        }

        return obj;
    }

    public void ReturnPooledObject(string poolName, GameObject obj)
    {
        UIPool pool = GetPool(poolName);
        if (pool == null || obj == null) return;

        if (pool.activeObjects.Contains(obj))
        {
            pool.activeObjects.Remove(obj);
            obj.SetActive(false);
            pool.pool.Enqueue(obj);

            // Limpiar animación si existe
            if (_activeAnimations.ContainsKey(obj))
            {
                if (_activeAnimations[obj] != null)
                {
                    StopCoroutine(_activeAnimations[obj]);
                }
                _activeAnimations.Remove(obj);
            }
        }
    }

    private UIPool GetPool(string poolName)
    {
        if (poolName == _damageTextPool.poolName) return _damageTextPool;
        if (poolName == _popupPool.poolName) return _popupPool;
        return null;
    }
    #endregion

    #region Pool Usage Methods
    public void ShowDamageText(Vector3 worldPosition, float damage, Color color)
    {
        GameObject damageObj = GetPooledObject("DamageText");
        if (damageObj == null) return;

        TextMeshProUGUI text = damageObj.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"-{damage:F1}";
            text.color = color;

            // Posicionamiento optimizado
            SetDamageTextPosition(damageObj, worldPosition);

            // Iniciar animación
            Coroutine animationCoroutine = StartCoroutine(AnimateDamageText(damageObj));
            _activeAnimations[damageObj] = animationCoroutine;
        }

        StartCoroutine(ReturnObjectAfterDelay("DamageText", damageObj, 2f));
    }

    private void SetDamageTextPosition(GameObject damageObj, Vector3 worldPosition)
    {
        Canvas canvas = damageObj.GetComponentInParent<Canvas>();
        if (canvas?.renderMode == RenderMode.ScreenSpaceOverlay && Camera.main != null)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPos, canvas.worldCamera, out Vector2 localPos))
            {
                localPos += new Vector2(Random.Range(-20f, 20f), Random.Range(10f, 30f));
                damageObj.transform.localPosition = localPos;
            }
        }
        else
        {
            damageObj.transform.position = worldPosition + Vector3.up;
        }
    }

    private IEnumerator AnimateDamageText(GameObject damageText)
    {
        if (damageText == null) yield break;

        Transform textTransform = damageText.transform;
        TextMeshProUGUI textComponent = damageText.GetComponent<TextMeshProUGUI>();

        if (textTransform == null || textComponent == null) yield break;

        Vector3 startPos = textTransform.localPosition;
        Vector3 endPos = startPos + Vector3.up * 50f;
        Color startColor = textComponent.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        const float duration = 1.5f;
        float elapsed = 0f;

        while (elapsed < duration && damageText != null && textComponent != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            textTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            textComponent.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        // Cleanup
        if (_activeAnimations.ContainsKey(damageText))
        {
            _activeAnimations.Remove(damageText);
        }
    }

    public void ShowPopup(string message, Vector3 position)
    {
        GameObject popup = GetPooledObject("Popup");
        if (popup != null)
        {
            TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = message;
            }

            popup.transform.position = position;
            StartCoroutine(ReturnObjectAfterDelay("Popup", popup, 3f));
        }
    }

    private IEnumerator ReturnObjectAfterDelay(string poolName, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            ReturnPooledObject(poolName, obj);
        }
    }
    #endregion

    #region UI Element Management
    private void FindUIElements()
    {
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button button in allButtons)
        {
            RegisterButtonByTag(button);
        }
        // Buscar elementos usando tags de manera optimizada
        var buttonMappings = new Dictionary<string, System.Action>
        {
            { "PlayButton", () => LoadScene("Level_01") },
            { "Level1Button", () => LoadScene("Level1") },
            { "Level2Button", () => LoadScene("Level2") },
            { "Level3Button", () => LoadScene("Level3") },
            { "OptionsButton", () => LoadScene("Options") },
            { "MainMenuButton", () => LoadScene("MenuScene") },
            { "PauseButton", PauseGame },
            { "ResumeButton", ResumeGame },
            { "QuitButton", QuitGame },
            { "MuteButton", ToggleMute },
            { "NextLevelButton", NextLevel },
        { "RetryButton", RetryLevel },
        { "VictoryMenuButton", () => LoadScene("MenuScene") },
        { "DefeatMenuButton", () => LoadScene("MenuScene") }
        };


        foreach (var kvp in buttonMappings)
        {
            FindAndRegisterButton(kvp.Key, kvp.Value);
        }

        // Textos con TextMeshPro
        string[] textTags = { "HealthText", "ScoreText", "LevelText" };
        foreach (string tag in textTags)
        {
            FindAndRegisterText(tag);
        }

        // Paneles
        string[] panelTags = { "PausePanel", "GameOverPanel" };
        foreach (string tag in panelTags)
        {
            FindAndRegisterPanel(tag);
        }

        // Sliders
        FindAndRegisterSlider("VolumeSlider", SetVolume);
    }
    private void RegisterButtonByTag(Button button)
    {
        string tag = button.tag;
        if (string.IsNullOrEmpty(tag)) return;

        System.Action callback = tag switch
        {
            "PlayButton" => () => LoadScene("PrototypeScene"),
            "Level1Button" => () => LoadScene("Level1"),
            "Level2Button" => () => LoadScene("Level2"),
            "Level3Button" => () => LoadScene("Level3"),
            "OptionsButton" => () => LoadScene("Options"),
            "MainMenuButton" => () => LoadScene("MenuScene"),
            "PauseButton" => PauseGame,
            "ResumeButton" => ResumeGame,
            "QuitButton" => QuitGame,
            "MuteButton" => ToggleMute,
            "NextLevelButton" => NextLevel,
            "RetryButton" => RetryLevel,
            "VictoryMenuButton" => () => LoadScene("MenuScene"),
            "DefeatMenuButton" => () => LoadScene("MenuScene"),
            _ => null
        };

        if (callback != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(callback.Invoke);
            _buttons[tag] = button;
            Debug.Log($"Botón registrado: {tag} en {button.gameObject.name}");
        }
    }
    private void NextLevel()
    {
        // Usar el nombre configurado en el inspector si está disponible
        if (!string.IsNullOrEmpty(_nextLevelName))
        {
            Debug.Log($"Cargando próximo nivel configurado: {_nextLevelName}");
            LoadScene(_nextLevelName);
        }
        else
        {
            // Fallback a la lógica anterior si no está configurado
            string currentScene = SceneManager.GetActiveScene().name;
            string nextLevel = GetNextLevelName(currentScene);
            Debug.Log($"Cargando próximo nivel automático: {nextLevel}");
            if (!string.IsNullOrEmpty(nextLevel))
            {
                LoadScene(nextLevel);
            }
            else
            {
                Debug.LogWarning("No se pudo determinar el próximo nivel");
            }
        }
    }
    private void RetryLevel()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"Recargando escena: {currentScene}");

        // Restaurar tiempo antes de recargar
        Time.timeScale = 1f;

        LoadScene(currentScene);
    }

    private string GetNextLevelName(string currentLevel)
    {
        switch (currentLevel)
        {
            case "Level_01": return "Level_02";
            case "Level_02": return "Level_03";
            case "Level_03": return null; // Último nivel, no hay siguiente
            default: return null;
        }
    }
    private void FindAndRegisterButton(string tagName, System.Action callback)
    {
        GameObject buttonObj = FindGameObjectByTag(tagName);

        if (buttonObj?.GetComponent<Button>() is Button button)
        {
            if (button == null) Debug.LogError($"BOTÓN NO ENCONTRADO: {tagName}");
            _buttons[tagName] = button;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(callback.Invoke);
        }

    }

    private void FindAndRegisterText(string tagName)
    {
        GameObject textObj = FindGameObjectByTag(tagName);
        if (textObj?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI text)
        {
            _texts[tagName] = text;
        }
    }

    private void FindAndRegisterPanel(string tagName)
    {
        GameObject panel = FindGameObjectByTag(tagName);
        if (panel != null)
        {
            _panels[tagName] = panel;
        }
    }

    private void FindAndRegisterSlider(string tagName, System.Action<float> callback)
    {
        GameObject sliderObj = FindGameObjectByTag(tagName);
        if (sliderObj?.GetComponent<Slider>() is Slider slider)
        {
            _sliders[tagName] = slider;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(callback.Invoke);
        }
    }

    private GameObject FindGameObjectByTag(string tag)
    {
        try
        {
            return GameObject.FindGameObjectWithTag(tag);
        }
        catch (UnityException)
        {
            return null;
        }
    }
    #endregion

    #region Getters
    public Button GetButton(string name) => _buttons.TryGetValue(name, out Button button) ? button : null;
    public TextMeshProUGUI GetText(string name) => _texts.TryGetValue(name, out TextMeshProUGUI text) ? text : null;
    public GameObject GetPanel(string name) => _panels.TryGetValue(name, out GameObject panel) ? panel : null;
    public Slider GetSlider(string name) => _sliders.TryGetValue(name, out Slider slider) ? slider : null;
    #endregion

    #region Game Flow & Scene Management
    private void SetupButtonListeners()
    {
        // Los listeners se configuran en FindAndRegisterButton
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        Time.timeScale = 1f;

        if (SceneLoaderManager.Instance.IsSceneConfigured(sceneName))
        {
            SceneLoaderManager.Instance.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void PauseGame()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        ShowCanvas("PauseCanvas");
    }

    private void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        HideCanvas("PauseCanvas");
    }


    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void Tick(float deltaTime)
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused) ResumeGame();
            else PauseGame();
        }
    }
    #endregion

    #region Health System
    private void SetupHealthSystem()
    {
        if (_healthSystem != null)
        {
            _healthSystem.OnHealthChanged -= UpdateHealthUI;
        }

        _healthSystem = ServiceLocator.Instance.GetService<HealthSystem>();
        if (_healthSystem != null)
        {
            _healthSystem.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(_healthSystem.GetCurrentHealth(), _healthSystem.GetMaxHealth());
        }
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        TextMeshProUGUI healthText = GetText("HealthText");
        if (healthText != null)
        {
            healthText.text = $"{Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
        }
    }
    #endregion

    #region Audio
    private void ToggleMute()
    {
        AudioListener.pause = !AudioListener.pause;
    }

    private void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("Volume", volume);
    }
    #endregion

    #region Cleanup
    private void CleanupActiveAnimations()
    {
        foreach (var kvp in _activeAnimations)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        _activeAnimations.Clear();
        CleanupPoolObjects();
    }

    private void CleanupPoolObjects()
    {
        CleanupPool(_damageTextPool, "DamageText");
        CleanupPool(_popupPool, "Popup");
    }

    private void CleanupPool(UIPool pool, string poolName)
    {
        if (pool?.activeObjects == null) return;

        for (int i = pool.activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = pool.activeObjects[i];
            if (obj != null)
            {
                ReturnPooledObject(poolName, obj);
            }
        }
    }

    private void ClearReferences()
    {
        // Limpiar listeners
        foreach (var button in _buttons.Values)
        {
            if (button != null) button.onClick.RemoveAllListeners();
        }

        foreach (var slider in _sliders.Values)
        {
            if (slider != null) slider.onValueChanged.RemoveAllListeners();
        }

        // Limpiar diccionarios
        _buttons.Clear();
        _texts.Clear();
        _panels.Clear();
        _sliders.Clear();
        _canvases.Clear();

        // Limpiar caches
        _canvasRaycastersCache.Clear();
        _canvasInteractiveCache.Clear();
    }

    private void OnDestroy()
    {
        CleanupActiveAnimations();

        if (_healthSystem != null)
        {
            _healthSystem.OnHealthChanged -= UpdateHealthUI;
        }

        UpdateManager.Instance.UnregisterStartable(this);
        ServiceLocator.Instance.Unregister<UIManager>();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UpdateManager.Instance.Unregister(this);
        ClearReferences();
    }
    #endregion
}