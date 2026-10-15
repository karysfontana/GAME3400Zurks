
using UnityEngine;
using UnityEngine.Events;

public class PhoneInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform player;
    [SerializeField] private float interactionDistance = 2f;

    [Header("Events")]
    public UnityEvent onPhonePickedUp;
    public UnityEvent onPhonePutDown; 

    public static bool isPickedUp = false;

    public void TryInteract()
{
    if (player == null)
    {
        Debug.LogError("Player is not assigned!");
        return;
    }

    float distance = Vector3.Distance(
        player.position,
        transform.position
    );

    Debug.Log("Distance to phone: " + distance);

    if (isPickedUp == false)
    {
        if (distance <= interactionDistance)
        {
            PickUpPhone();
        }
    }
    else
    {
        if (distance <= interactionDistance)
        {
            PutDownPhone();
        }
    }
}

    private void PickUpPhone()
    {
        isPickedUp = true;

        Debug.Log("Player picked up the phone!");

        onPhonePickedUp?.Invoke();
    }

    private void PutDownPhone()
    {
        isPickedUp = false; 

        Debug.Log($"Player put phone down."); 
        onPhonePutDown?.Invoke(); 
    }
}
