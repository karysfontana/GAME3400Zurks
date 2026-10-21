
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactDistance = 3f;

    public void Interact(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        RaycastHit hit;

        if (Physics.Raycast(
            playerCamera.transform.position,
            playerCamera.transform.forward,
            out hit,
            interactDistance))
        {
            SceneSwitcher switcher =
                hit.collider.GetComponentInParent<SceneSwitcher>();

            if (switcher != null)
            {
                switcher.SwitchScene();
            }
        }
    }
}

