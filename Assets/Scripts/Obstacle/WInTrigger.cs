using UnityEngine;

public class WinTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerSetup playerSetup = other.GetComponent<PlayerSetup>();
        if (playerSetup != null)
        {
            UIManager uiManager = ServiceLocator.Instance.GetService<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowVictoryCanvas();
            }
        }
    }
}

