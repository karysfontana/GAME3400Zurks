
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    [SerializeField] private string targetScene = "Barrel";

    public void SwitchScene()
    {
        SceneManager.LoadScene(targetScene);
    }
}

