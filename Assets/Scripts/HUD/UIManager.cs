
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class UIManager : MonoBehaviour, IStartable
{
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UIOptimizationManager");
                _instance = go.AddComponent<UIManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Canvas Management")]
    [SerializeField] private List<CanvasInfo> _canvasInfos = new List<CanvasInfo>();

    [Header("UI Pooling")]
    [SerializeField] private UIPool _damageTextPool;
    [SerializeField] private UIPool _popupPool;

    // Referencias dinámicas que se encuentran en cada escena
    private Dictionary<string, Button> _buttons = new Dictionary<string, Button>();
    private Dictionary<string, TextMeshProUGUI> _texts = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, GameObject> _panels = new Dictionary<string, GameObject>();
    private Dictionary<string, Slider> _sliders = new Dictionary<string, Slider>();
    private Dictionary<string, Canvas> _canvases = new Dictionary<string, Canvas>();
    private Dictionary<GameObject, Coroutine> _activeAnimations = new Dictionary<GameObject, Coroutine>();

    private HealthSystem _healthSystem;
    private bool _isPaused = false;
    private bool _hasInitialized = false;

    // NUEVA CONFIGURACIÓN PARA MANEJO AUTOMÁTICO DE CANVAS POR ESCENA
    [Header("Scene-Specific Canvas Settings")]
    [SerializeField] private List<SceneCanvasConfig> _sceneCanvasConfigs = new List<SceneCanvasConfig>();

    [System.Serializable]
    public class SceneCanvasConfig
    {
        public string sceneName;
        public string damageTextCanvasName = "GameCanvas"; // Nombre del canvas donde van los damage texts
        public string popupCanvasName = "UICanvas"; // Nombre del canvas donde van los popups
    }

    [System.Serializable]
    public class CanvasInfo
    {
        public string canvasName;
        public bool isStatic;
        public bool startActive;
        [Tooltip("Si es dinámico, se oculta cuando no se usa")]
        public bool hideWhenInactive;
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
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            UpdateManager.Instance.RegisterStartable(this);
            ServiceLocator.Instance.Register<UIManager>(this);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
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

    private void CleanupActiveAnimations()
    {
        // Detener todas las corrutinas activas
        foreach (var kvp in _activeAnimations)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        _activeAnimations.Clear();

        // Limpiar objetos activos en pools
        CleanupPoolObjects();
    }

    private void CleanupPoolObjects()
    {
        // Retornar todos los objetos activos al pool
        if (_damageTextPool.activeObjects != null)
        {
            for (int i = _damageTextPool.activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = _damageTextPool.activeObjects[i];
                if (obj != null)
                {
                    ReturnPooledObject("DamageText", obj);
                }
            }
        }

        if (_popupPool.activeObjects != null)
        {
            for (int i = _popupPool.activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = _popupPool.activeObjects[i];
                if (obj != null)
                {
                    ReturnPooledObject("Popup", obj);
                }
            }
        }
    }

    private void SetupCurrentScene()
    {
        ClearReferences();
        FindUIElements();
        SetupCanvasOptimization();
        UpdatePoolParentTransforms(); // NUEVA FUNCIÓN - Actualizar parent transforms según la escena
        InitializeUIPools();
        SetupButtonListeners();
        SetupHealthSystem();
    }

    // NUEVA FUNCIÓN: Actualizar parent transforms de los pools según la escena actual
    private void UpdatePoolParentTransforms()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"Actualizando pools para escena: {currentSceneName}");

        // Listar todos los canvas disponibles para debug
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        Debug.Log($"Canvas encontrados en la escena:");
        foreach (Canvas c in allCanvases)
        {
            Debug.Log($"  - {c.name} (Activo: {c.gameObject.activeSelf})");
        }

        // Buscar configuración para la escena actual
        SceneCanvasConfig sceneConfig = _sceneCanvasConfigs.Find(config => config.sceneName == currentSceneName);

        if (sceneConfig != null)
        {
            Debug.Log($"Configuración encontrada para {currentSceneName}: DamageText->{sceneConfig.damageTextCanvasName}, Popup->{sceneConfig.popupCanvasName}");
            // Actualizar parent transform del damage text pool
            UpdatePoolParentTransform(_damageTextPool, sceneConfig.damageTextCanvasName);

            // Actualizar parent transform del popup pool
            UpdatePoolParentTransform(_popupPool, sceneConfig.popupCanvasName);
        }
        else
        {
            Debug.Log($"No se encontró configuración específica para {currentSceneName}, usando configuración automática");
            // Configuración automática inteligente
            UpdatePoolParentTransformAuto(_damageTextPool, "DamageText");
            UpdatePoolParentTransformAuto(_popupPool, "Popup");
        }
    }

    private void UpdatePoolParentTransform(UIPool pool, string canvasName)
    {
        if (pool == null) return;

        // Buscar el canvas específico en la escena actual
        Canvas targetCanvas = FindCanvasByName(canvasName);

        if (targetCanvas != null)
        {
            pool.parentTransform = targetCanvas.transform;
            Debug.Log($"✓ Pool '{pool.poolName}' parent transform actualizado a canvas: {canvasName}");
        }
        else
        {
            Debug.LogWarning($"Canvas '{canvasName}' no encontrado para pool '{pool.poolName}'. Intentando configuración automática...");
            // Usar configuración automática como fallback
            UpdatePoolParentTransformAuto(pool, pool.poolName);
        }

        // Reasignar todos los objetos existentes del pool al nuevo parent
        ReassignPoolObjectsToNewParent(pool);
    }

    // NUEVA FUNCIÓN: Configuración automática de canvas basada en el tipo de pool
    private void UpdatePoolParentTransformAuto(UIPool pool, string poolType)
    {
        Canvas targetCanvas = null;
        Canvas[] canvases = FindObjectsOfType<Canvas>();

        // Estrategia de búsqueda basada en el tipo de pool
        if (poolType.ToLower().Contains("damage"))
        {
            // Para damage text, buscar canvas de juego/gameplay
            targetCanvas = System.Array.Find(canvases, c =>
                c.name.ToLower().Contains("game") ||
                c.name.ToLower().Contains("hud") ||
                c.name.ToLower().Contains("gameplay") ||
                c.name.ToLower().Contains("world") ||
                c.name.ToLower().Contains("dynamic") ||
                c.renderMode == RenderMode.WorldSpace);
        }
        else if (poolType.ToLower().Contains("popup"))
        {
            // Para popups, buscar canvas de UI
            targetCanvas = System.Array.Find(canvases, c =>
                c.name.ToLower().Contains("ui") ||
                c.name.ToLower().Contains("menu") ||
                c.name.ToLower().Contains("interface") ||
                c.renderMode == RenderMode.ScreenSpaceOverlay);
        }

        // Si no encuentra canvas específico, usar el primero disponible que esté activo
        if (targetCanvas == null)
        {
            targetCanvas = System.Array.Find(canvases, c => c.gameObject.activeSelf);
        }

        // Último recurso: usar cualquier canvas
        if (targetCanvas == null && canvases.Length > 0)
        {
            targetCanvas = canvases[0];
        }

        if (targetCanvas != null)
        {
            pool.parentTransform = targetCanvas.transform;
            Debug.Log($"✓ Pool '{pool.poolName}' asignado automáticamente a canvas: {targetCanvas.name}");
        }
        else
        {
            Debug.LogError($"❌ No se encontró ningún canvas disponible para el pool '{pool.poolName}'");
        }
    }

    private Canvas FindCanvasByName(string canvasName)
    {
        // Primero buscar en el diccionario de canvas registrados
        if (_canvases.ContainsKey(canvasName))
        {
            Canvas cachedCanvas = _canvases[canvasName];
            if (cachedCanvas != null && cachedCanvas.gameObject != null)
            {
                return cachedCanvas;
            }
            else
            {
                // Limpiar referencia inválida
                _canvases.Remove(canvasName);
            }
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

        // Búsqueda más amplia: buscar canvas que contengan el nombre
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.name.ToLower().Contains(canvasName.ToLower()) ||
                canvasName.ToLower().Contains(canvas.name.ToLower()))
            {
                _canvases[canvasName] = canvas;
                Debug.Log($"Canvas encontrado por coincidencia parcial: '{canvas.name}' para búsqueda '{canvasName}'");
                return canvas;
            }
        }

        return null;
    }

    private void ReassignPoolObjectsToNewParent(UIPool pool)
    {
        if (pool.parentTransform == null) return;

        // Reasignar objetos en la cola
        GameObject[] pooledObjects = pool.pool.ToArray();
        foreach (GameObject obj in pooledObjects)
        {
            if (obj != null)
            {
                obj.transform.SetParent(pool.parentTransform);
            }
        }

        // Reasignar objetos activos
        foreach (GameObject obj in pool.activeObjects)
        {
            if (obj != null)
            {
                obj.transform.SetParent(pool.parentTransform);
            }
        }
    }

    #region Canvas Optimization
    private void SetupCanvasOptimization()
    {
        // Encontrar todos los Canvas en la escena
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();

        foreach (Canvas canvas in allCanvases)
        {
            string canvasName = canvas.gameObject.name;
            _canvases[canvasName] = canvas;

            // Buscar configuración para este canvas
            CanvasInfo info = _canvasInfos.Find(c => c.canvasName == canvasName);
            if (info != null)
            {
                OptimizeCanvas(canvas, info);
            }
            else
            {
                // Configuración por defecto
                OptimizeCanvas(canvas, new CanvasInfo
                {
                    canvasName = canvasName,
                    isStatic = false,
                    startActive = canvas.gameObject.activeSelf,
                    hideWhenInactive = true
                });
            }
        }
    }

    private void OptimizeCanvas(Canvas canvas, CanvasInfo info)
    {
        // Configurar Canvas según si es estático o dinámico
        if (info.isStatic)
        {
            // Canvas estático - optimizar para elementos que no cambian
            canvas.overrideSorting = false;
            canvas.pixelPerfect = true;

            // Desactivar raycaster si no necesita interacción
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null && !HasInteractiveElements(canvas))
            {
                raycaster.enabled = false;
            }
        }
        else
        {
            // Canvas dinámico - optimizar para elementos que cambian frecuentemente
            canvas.overrideSorting = true;
            canvas.pixelPerfect = false;
        }

        // Configurar estado inicial
        canvas.gameObject.SetActive(info.startActive);

        // Si es un canvas que se oculta cuando no está activo
        if (info.hideWhenInactive && !info.startActive)
        {
            HideCanvas(info.canvasName);
        }
    }

    private bool HasInteractiveElements(Canvas canvas)
    {
        // Verificar si el canvas tiene elementos interactivos
        Button[] buttons = canvas.GetComponentsInChildren<Button>();
        Slider[] sliders = canvas.GetComponentsInChildren<Slider>();
        Toggle[] toggles = canvas.GetComponentsInChildren<Toggle>();

        return buttons.Length > 0 || sliders.Length > 0 || toggles.Length > 0;
    }

    public void ShowCanvas(string canvasName)
    {
        if (_canvases.ContainsKey(canvasName))
        {
            Canvas canvas = _canvases[canvasName];
            canvas.gameObject.SetActive(true);

            // Reactivar raycaster si existe
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = true;
            }
        }
    }

    public void HideCanvas(string canvasName)
    {
        if (_canvases.ContainsKey(canvasName))
        {
            Canvas canvas = _canvases[canvasName];
            canvas.gameObject.SetActive(false);

            // Desactivar raycaster para ahorrar rendimiento
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }
        }
    }
    #endregion

    #region UI Object Pooling
    private void InitializeUIPools()
    {
        // Inicializar pool de texto de daño
        if (_damageTextPool.prefab != null)
        {
            InitializePool(_damageTextPool);
        }

        // Inicializar pool de popups
        if (_popupPool.prefab != null)
        {
            InitializePool(_popupPool);
        }
    }

    private void InitializePool(UIPool pool)
    {
        // NO limpiar la cola si ya tiene objetos (para mantener objetos existentes al cambiar de escena)
        if (pool.pool == null)
            pool.pool = new Queue<GameObject>();

        if (pool.activeObjects == null)
            pool.activeObjects = new List<GameObject>();

        // Si no hay parent transform, usar configuración por defecto
        if (pool.parentTransform == null)
        {
            GameObject parent = GameObject.Find(pool.poolName + "_Parent");
            if (parent == null)
            {
                parent = new GameObject(pool.poolName + "_Parent");
                DontDestroyOnLoad(parent);
            }
            pool.parentTransform = parent.transform;
        }

        // Solo crear objetos nuevos si el pool está vacío
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
        if (pool == null) return;

        if (pool.activeObjects.Contains(obj))
        {
            pool.activeObjects.Remove(obj);
            obj.SetActive(false);
            pool.pool.Enqueue(obj);
        }
    }

    private UIPool GetPool(string poolName)
    {
        if (poolName == _damageTextPool.poolName) return _damageTextPool;
        if (poolName == _popupPool.poolName) return _popupPool;
        return null;
    }

    // Métodos de conveniencia para usar los pools
    public void ShowDamageText(Vector3 worldPosition, float damage, Color color)
    {
        GameObject damageObj = GetPooledObject("DamageText");
        if (damageObj != null)
        {
            TextMeshProUGUI text = damageObj.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = "-" + damage.ToString("F1");
                text.color = color;

                // Convertir posición del mundo a UI
                Canvas canvas = damageObj.GetComponentInParent<Canvas>();
                if (canvas != null && Camera.main != null)
                {
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
                    Vector2 localPos;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvas.transform as RectTransform, screenPos, canvas.worldCamera, out localPos);

                    localPos += new Vector2(UnityEngine.Random.Range(-20f, 20f), UnityEngine.Random.Range(10f, 30f));
                    damageObj.transform.localPosition = localPos;
                }
                else
                {
                    damageObj.transform.position = worldPosition + Vector3.up * 1f;
                }

                // Iniciar animación con seguimiento
                Coroutine animationCoroutine = StartCoroutine(AnimateDamageText(damageObj));
                _activeAnimations[damageObj] = animationCoroutine;
            }

            // Auto-retornar después de un tiempo
            StartCoroutine(ReturnDamageTextAfterDelay(damageObj, 2f));
        }
    }

    // CORRUTINA MEJORADA CON VERIFICACIONES DE REFERENCIAS
    private IEnumerator AnimateDamageText(GameObject damageText)
    {
        if (damageText == null) yield break;

        Vector3 startPos = damageText.transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * 50f;

        TextMeshProUGUI text = damageText.GetComponent<TextMeshProUGUI>();
        if (text == null) yield break;

        Color startColor = text.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float duration = 1.5f;
        float elapsed = 0f;

        while (elapsed < duration && damageText != null && text != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Verificar que los objetos aún existen
            if (damageText == null || text == null) break;

            // Interpolar posición
            damageText.transform.localPosition = Vector3.Lerp(startPos, endPos, t);

            // Interpolar color (fade out)
            text.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        // Limpiar referencia al finalizar
        if (damageText != null && _activeAnimations.ContainsKey(damageText))
        {
            _activeAnimations.Remove(damageText);
        }
    }

    private IEnumerator ReturnDamageTextAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Verificar que el objeto aún existe antes de retornarlo
        if (obj != null)
        {
            ReturnPooledObject("DamageText", obj);
        }
    }

    private IEnumerator ReturnPopupAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null)
        {
            ReturnPooledObject("Popup", obj);
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
            StartCoroutine(ReturnPopupAfterDelay(popup, 3f));
        }
    }
    #endregion

    #region Find and Register Methods (usando TextMeshPro)
    private void FindUIElements()
    {
        // Buscar botones por tag
        FindAndRegisterButton("PlayButton", () => LoadScene("PrototypeScene"));
        FindAndRegisterButton("Level1Button", () => LoadScene("Level1"));
        FindAndRegisterButton("Level2Button", () => LoadScene("Level2"));
        FindAndRegisterButton("Level3Button", () => LoadScene("Level3"));
        FindAndRegisterButton("OptionsButton", () => LoadScene("Options"));
        FindAndRegisterButton("MainMenuButton", () => LoadScene("MenuScene"));
        FindAndRegisterButton("PauseButton", PauseGame);
        FindAndRegisterButton("ResumeButton", ResumeGame);
        FindAndRegisterButton("QuitButton", QuitGame);
        FindAndRegisterButton("MuteButton", ToggleMute);

        // Buscar textos por tag (TextMeshPro)
        FindAndRegisterText("HealthText");
        FindAndRegisterText("ScoreText");
        FindAndRegisterText("LevelText");

        // Buscar paneles por tag  
        FindAndRegisterPanel("PausePanel");
        FindAndRegisterPanel("GameOverPanel");

        // Buscar sliders por tag
        FindAndRegisterSlider("VolumeSlider", SetVolume);
    }

    private void FindAndRegisterButton(string name, System.Action callback)
    {
        try
        {
            GameObject buttonObj = GameObject.FindGameObjectWithTag(name);

            if (buttonObj != null)
            {
                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    _buttons[name] = button;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => callback());
                }
            }
        }
        catch (UnityException)
        {
            Debug.LogWarning($"Tag '{name}' no encontrado para botón");
        }
    }

    private void FindAndRegisterText(string name)
    {
        try
        {
            GameObject textObj = GameObject.FindGameObjectWithTag(name);

            if (textObj != null)
            {
                // Usar TextMeshProUGUI en lugar de TMP_Text genérico
                TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    _texts[name] = text;
                }
            }
        }
        catch (UnityException)
        {
            Debug.LogWarning($"Tag '{name}' no encontrado para texto");
        }
    }

    private void FindAndRegisterPanel(string name)
    {
        try
        {
            GameObject panel = GameObject.FindGameObjectWithTag(name);

            if (panel != null)
            {
                _panels[name] = panel;
            }
        }
        catch (UnityException)
        {
            Debug.LogWarning($"Tag '{name}' no encontrado para panel");
        }
    }

    private void FindAndRegisterSlider(string name, System.Action<float> callback)
    {
        try
        {
            GameObject sliderObj = GameObject.FindGameObjectWithTag(name);

            if (sliderObj != null)
            {
                Slider slider = sliderObj.GetComponent<Slider>();
                if (slider != null)
                {
                    _sliders[name] = slider;
                    slider.onValueChanged.RemoveAllListeners();
                    slider.onValueChanged.AddListener((float value) => callback(value));
                }
            }
        }
        catch (UnityException)
        {
            Debug.LogWarning($"Tag '{name}' no encontrado para slider");
        }
    }
    #endregion

    #region Getter Methods
    public Button GetButton(string name) => _buttons.ContainsKey(name) ? _buttons[name] : null;
    public TextMeshProUGUI GetText(string name) => _texts.ContainsKey(name) ? _texts[name] : null;
    public GameObject GetPanel(string name) => _panels.ContainsKey(name) ? _panels[name] : null;
    public Slider GetSlider(string name) => _sliders.ContainsKey(name) ? _sliders[name] : null;
    #endregion

    private void SetupButtonListeners()
    {
        // Los listeners ya se configuran en FindAndRegisterButton
    }

    #region Scene Management
    private void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
    #endregion

    #region Game Flow
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
                ResumeGame();
            else
                PauseGame();
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
            healthText.text = $"HP: {Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
        }
    }
    #endregion

    #region Audio Controls
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
    private void ClearReferences()
    {
        foreach (var button in _buttons.Values)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }

        foreach (var slider in _sliders.Values)
        {
            if (slider != null)
                slider.onValueChanged.RemoveAllListeners();
        }

        _buttons.Clear();
        _texts.Clear();
        _panels.Clear();
        _sliders.Clear();
        _canvases.Clear();
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
        ClearReferences();
    }
    #endregion
}
