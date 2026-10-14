using UnityEngine;

public class QuitMenuController : MonoBehaviour
{
    public GameObject quitMenu;

    public void OpenQuitMenu()
    {
        quitMenu.SetActive(true);
    }

    public void CloseQuitMenu()
    {
        quitMenu.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}