using UnityEngine;
using UnityEngine.AI;

public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject gameOverPanel;

    [Header("Player")]
    public Transform respawnPoint;

    [Header("Zurk")]
    public Transform zurk;

    private PlayerController player;

    private Vector3 zurkStartPosition;
    private Quaternion zurkStartRotation;

    void Start()
    {
        // Hide Game Over UI when the game starts
        gameOverPanel.SetActive(false);

        // Save Zurk's starting position
        if (zurk != null)
        {
            zurkStartPosition = zurk.position;
            zurkStartRotation = zurk.rotation;
        }
    }

    public void GameOver(PlayerController playerController)
    {
        player = playerController;

        if (player != null)
        {
            player.StopPlayer();
        }

        gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        if (player != null)
        {
            player.Respawn(respawnPoint.position);
        }

        if (zurk != null)
        {
            NavMeshAgent agent = zurk.GetComponent<NavMeshAgent>();

            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(zurkStartPosition);
            }
            else
            {
                zurk.position = zurkStartPosition;
            }

            zurk.rotation = zurkStartRotation;
        }

        gameOverPanel.SetActive(false);
    }
}
