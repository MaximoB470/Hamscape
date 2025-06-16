using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DamageTextTester : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private float testInterval = 1f;
    [SerializeField] private float damageRange = 50f;
    [SerializeField] private Vector3 testPosition = Vector3.zero;
    [SerializeField] private bool autoTest = false;

    [Header("Manual Test")]
    [SerializeField] private Button testButton;

    private float lastTestTime;

    void Start()
    {
        // Configurar bot?n de prueba manual si existe
        if (testButton != null)
        {
            testButton.onClick.AddListener(TestDamageText);
        }

        // Si no hay bot?n, buscar uno por tag
        if (testButton == null)
        {
            GameObject buttonObj = GameObject.FindGameObjectWithTag("TestDamageButton");
            if (buttonObj != null)
            {
                testButton = buttonObj.GetComponent<Button>();
                if (testButton != null)
                {
                    testButton.onClick.AddListener(TestDamageText);
                }
            }
        }
    }

    void Update()
    {
        // Auto-test si est? habilitado
        if (autoTest && Time.time - lastTestTime >= testInterval)
        {
            TestDamageText();
            lastTestTime = Time.time;
        }

        // Test manual con tecla T
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestDamageText();
        }

        // Test con clic del mouse (convertir posici?n)
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));
            TestDamageTextAtPosition(worldPos);
        }
    }

    public void TestDamageText()
    {
        TestDamageTextAtPosition(testPosition);
    }

    public void TestDamageTextAtPosition(Vector3 worldPosition)
    {
        if (UIManager.Instance != null)
        {
            // Generar da?o aleatorio
            float damage = Random.Range(10f, damageRange);

            // Seleccionar color basado en el da?o
            Color damageColor = GetDamageColor(damage);

            // Mostrar el texto de da?o
            UIManager.Instance.ShowDamageText(worldPosition, damage, damageColor);

            Debug.Log($"Damage Text mostrado: {damage:F0} en posici?n {worldPosition}");
        }
        else
        {
            Debug.LogError("UIManager.Instance es null!");
        }
    }

    private Color GetDamageColor(float damage)
    {
        // Colores basados en la cantidad de da?o
        if (damage >= 40f)
            return Color.red;      // Da?o alto
        else if (damage >= 25f)
            return Color.yellow;   // Da?o medio
        else
            return Color.white;    // Da?o bajo
    }

    // M?todo para test desde el inspector
    [ContextMenu("Test Damage Text")]
    public void TestFromInspector()
    {
        TestDamageText();
    }
}