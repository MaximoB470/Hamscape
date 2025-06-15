using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour, IStartable
{
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UIManager");
                _instance = go.AddComponent<UIManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Referencias dinámicas que se encuentran en cada escena
    private Dictionary<string, Button> _buttons = new Dictionary<string, Button>();
    private Dictionary<string, TMP_Text> _texts = new Dictionary<string, TMP_Text>();
    private Dictionary<string, GameObject> _panels = new Dictionary<string, GameObject>();
    private Dictionary<string, Slider> _sliders = new Dictionary<string, Slider>();

    private HealthSystem _healthSystem;
    private bool _isPaused = false;
    private bool _hasInitialized = false;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            UpdateManager.Instance.RegisterStartable(this);
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
        if (_hasInitialized)
        {
            SetupCurrentScene();
        }
    }

    private void SetupCurrentScene()
    {
        // Limpiar referencias anteriores
        ClearReferences();

        // Buscar y configurar elementos UI en la escena actual
        FindUIElements();
        SetupButtonListeners();
        SetupHealthSystem();

        // Configurar UI inicial
        GameObject pausePanel = GetPanel("PausePanel");
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

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

        // Buscar textos por tag
        FindAndRegisterText("HealthText");

        // Buscar paneles por tag  
        FindAndRegisterPanel("PausePanel");

        // Buscar sliders por tag
        FindAndRegisterSlider("VolumeSlider", SetVolume);
    }

    #region Find and Register Methods
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
            // Tag no existe, ignorar silenciosamente
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
                TMP_Text text = textObj.GetComponent<TMP_Text>();
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

    private void FindAndRegisterSlider(string name, UnityEngine.Events.UnityAction<float> callback)
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
                    slider.onValueChanged.AddListener(callback);
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
    public TMP_Text GetText(string name) => _texts.ContainsKey(name) ? _texts[name] : null;
    public GameObject GetPanel(string name) => _panels.ContainsKey(name) ? _panels[name] : null;
    public Slider GetSlider(string name) => _sliders.ContainsKey(name) ? _sliders[name] : null;
    #endregion

    private void SetupButtonListeners()
    {
        // Los listeners ya se configuran en FindAndRegisterButton
        // Aquí puedes agregar lógica adicional si es necesaria
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
        GameObject pausePanel = GetPanel("PausePanel");
        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    private void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        GameObject pausePanel = GetPanel("PausePanel");
        if (pausePanel != null)
            pausePanel.SetActive(false);
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
        // Desconectar sistema anterior si existe
        if (_healthSystem != null)
        {
            _healthSystem.OnHealthChanged -= UpdateHealthUI;
        }

        // Conectar nuevo sistema de salud
        _healthSystem = ServiceLocator.Instance.GetService<HealthSystem>();
        if (_healthSystem != null)
        {
            _healthSystem.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(_healthSystem.GetCurrentHealth(), _healthSystem.GetMaxHealth());
        }
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        TMP_Text healthText = GetText("HealthText");
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
        // Limpiar listeners de botones anteriores
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
    }

    private void OnDestroy()
    {
        if (_healthSystem != null)
        {
            _healthSystem.OnHealthChanged -= UpdateHealthUI;
        }

        UpdateManager.Instance.UnregisterStartable(this);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ClearReferences();
    }
    #endregion
}