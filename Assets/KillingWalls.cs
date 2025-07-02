using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KillingWalls : MonoBehaviour
{

    [Header("UI Defeat Panel Name")]
    [SerializeField] private string defeatPanelName = "DefeatCanvas"; 

    private void OnTriggerEnter2D(Collider2D other)
    {
       
            PlayerSetup player = other.GetComponent<PlayerSetup>();

            if (player != null)
            {
                player.ApplyDamage(9999); 

                UIManager uiManager = ServiceLocator.Instance.GetService<UIManager>();
                if (uiManager != null)
                {
                    //uiManager.ShowPanel(defeatPanelName);
                    Debug.Log("Muelto");
                }
            }
        }
    
}
