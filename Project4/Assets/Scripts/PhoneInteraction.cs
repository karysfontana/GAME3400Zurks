
using UnityEngine;
using UnityEngine.Events;

public class PhoneInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform player;
    [SerializeField] private float interactionDistance = 2f;

    [Header("Events")]
    public UnityEvent onPhonePickedUp;

    private bool isPickedUp = false;

    public void TryInteract()
    {
        if (player == null || isPickedUp)
        {
            return;
        }

        float distance = Vector3.Distance(
            player.position,
            transform.position
        );

        if (distance <= interactionDistance)
        {
            PickUpPhone();
        }
    }

    private void PickUpPhone()
    {
        isPickedUp = true;

        Debug.Log("Player picked up the phone!");

        onPhonePickedUp?.Invoke();
    }
}
