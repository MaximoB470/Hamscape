using UnityEngine;
using System.Collections;


public class LifeSystem : MonoBehaviour, ICustomUpdate
{
    [SerializeField] private float maxLife = 100f;
    [SerializeField] private float regenPerSecond = 5f;
    [SerializeField] private float damagePerSecond = 10f;
    private UpdateManager _updateManager;
    private float currentLife;
    private Vector3 lastPosition;
    private bool isMoving;

    private void Start()
    {
        _updateManager = ServiceLocator.Instance.GetService<UpdateManager>();
        if (_updateManager != null)
        {
            _updateManager.Register(this);
        }
        else
        {
            Debug.LogError("UpdateManager not found in ServiceLocator");
        }

        currentLife = maxLife;
        lastPosition = transform.position;
    }
    public void CustomUpdate()
    {

        DetectMovement();

        if (isMoving)
        {
            currentLife += regenPerSecond * Time.deltaTime;
            Debug.Log("regen");
        }
        else
        {
            currentLife -= damagePerSecond * Time.deltaTime;
            Debug.Log("losing");
        }

        currentLife = Mathf.Clamp(currentLife, 0f, maxLife);

        if (currentLife <= 0f)
        {
            Die();
        }
    }
    private void DetectMovement()
    {
        isMoving = transform.position != lastPosition;
        lastPosition = transform.position;
    }
    private void Die()
    {
        Debug.Log("Player Died");
        Destroy(gameObject);

    }
    private void OnDestroy()
    {
        var updateManager = ServiceLocator.Instance.GetService<UpdateManager>();
        if (updateManager != null)
        {
            updateManager.Unregister(this);
        }
    }
}