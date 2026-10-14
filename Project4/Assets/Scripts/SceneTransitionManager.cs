
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName = "2020s";

    [Header("Trigger Settings")]
    [SerializeField] private bool useColliderTrigger = false;

    private bool isTransitioning = false;

    // Called when dialogue finishes or another event occurs
    public void TransitionToNextScene()
    {
        if (isTransitioning)
        {
            return;
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("Next scene is not assigned!");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogWarning("Scene cannot be loaded: " + nextSceneName);
            return;
        }

        isTransitioning = true;

        Debug.Log("Loading scene: " + nextSceneName);

        SceneManager.LoadScene(nextSceneName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useColliderTrigger)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            TransitionToNextScene();
        }
    }
}
