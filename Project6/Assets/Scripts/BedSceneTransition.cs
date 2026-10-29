using UnityEngine;
using UnityEngine.SceneManagement;

public class BedSceneTransition : MonoBehaviour
{
    [SerializeField] private string nextSceneName;

    private bool isLoading;

    public void GoToNextScene()
    {
        if (isLoading)
            return;

        string sceneName = nextSceneName.Trim();

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Please set Next Scene Name.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "Scene is missing from the Build Scene List: "
                + sceneName, this);
            return;
        }

        isLoading = true;
        SceneManager.LoadSceneAsync(
            sceneName, LoadSceneMode.Single);
    }
}
