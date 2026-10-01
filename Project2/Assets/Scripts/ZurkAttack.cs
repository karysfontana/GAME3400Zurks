using UnityEngine;

public class ZurkAttack : MonoBehaviour
{
    public GameOverManager gameOverManager;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Zurk touched: " + other.name);

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                Debug.Log("Zurk hit player!");
                gameOverManager.GameOver(player);
            }
        }
    }
}