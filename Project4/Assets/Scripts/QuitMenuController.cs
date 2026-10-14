using UnityEngine;
using UnityEngine.InputSystem;

public class QuitMenuController : MonoBehaviour
{
    public GameObject quitMenu;

    void Start()
    {
        // Game starts with the quit menu closed
        quitMenu.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleQuitMenu();
        }
    }

    public void ToggleQuitMenu()
    {
        quitMenu.SetActive(!quitMenu.activeSelf);
    }

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