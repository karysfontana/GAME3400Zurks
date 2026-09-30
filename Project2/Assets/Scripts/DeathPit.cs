using UnityEngine;

public class DeathPit : MonoBehaviour
{
    public GameObject gameOverPanel;
    public Transform respawnPoint;

    private PlayerController player;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                player.StopPlayer();
            }

            gameOverPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        if (player != null)
        {
            player.Respawn(respawnPoint.position);
        }

        gameOverPanel.SetActive(false);
    }
}