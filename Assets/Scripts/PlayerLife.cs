using UnityEngine;

public class PlayerLife : MonoBehaviour, ICustomUpdate
{
    public float maxLife = 10f;
    public float currentLife;
    public float damagePerSecond = 1f;
    public float regenPerSecond = 1f;

    private Vector3 lastPosition;
    private bool isMoving;

    private void Start()
    {
        currentLife = maxLife;
        lastPosition = transform.position;

        UpdateManager manager = FindObjectOfType<UpdateManager>();
        if (manager != null)
        {
            manager.Register(this);
        }
    }

    public void CustomUpdate()
    {
        DetectMovement();

        if (isMoving)
        {
            currentLife += regenPerSecond * Time.deltaTime;
            Debug.Log("Regenerando vida");
        }
        else
        {
            currentLife -= damagePerSecond * Time.deltaTime;
            Debug.Log("Perdiendo vida");
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
    }
}