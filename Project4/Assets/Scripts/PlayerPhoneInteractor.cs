
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPhoneInteractor : MonoBehaviour
{
    [SerializeField] private PhoneInteraction phone;

    void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (phone != null)
            {
                phone.TryInteract();
            }
        }
    }
}

