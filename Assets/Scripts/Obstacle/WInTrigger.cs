using UnityEngine;

public class WinTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerSetup playerSetup = other.GetComponent<PlayerSetup>();
        if (playerSetup != null)
        {
            Debug.Log("¡Ganaste!");
        }
    }
}

