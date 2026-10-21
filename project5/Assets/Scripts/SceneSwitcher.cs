
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    [SerializeField] private string targetScene = "WasherDrum";

    public void SwitchScene()
    {
        SceneManager.LoadScene(targetScene);
    }
}

