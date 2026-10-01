using UnityEngine;
using UnityEngine.AI;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel;
    public Transform respawnPoint;

    public Transform[] zurks;

    private PlayerController player;

    private Vector3[] zurkStartPositions;
    private Quaternion[] zurkStartRotations;

    void Start()
    {
        gameOverPanel.SetActive(false);

        zurkStartPositions = new Vector3[zurks.Length];
        zurkStartRotations = new Quaternion[zurks.Length];

        for (int i = 0; i < zurks.Length; i++)
        {
            zurkStartPositions[i] = zurks[i].position;
            zurkStartRotations[i] = zurks[i].rotation;
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
        
        for (int i = 0; i < zurks.Length; i++)
        {
            NavMeshAgent agent = zurks[i].GetComponent<NavMeshAgent>();

            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(zurkStartPositions[i]);
            }
            else
            {
                zurks[i].position = zurkStartPositions[i];
            }

            zurks[i].rotation = zurkStartRotations[i];
        }

        
        if (player != null)
        {
            player.Respawn(respawnPoint.position);
        }

        gameOverPanel.SetActive(false);
    }
}