using UnityEngine;

/*
   This script works such that the camera its attached to 
   will follow the players mouse movement-enabling FPS view. 
   Found it in my old project files & slightly updated it. 
*/
public class FPSCamScript : MonoBehaviour
{
    [SerializeField] private Transform player; 
    [SerializeField] private float mouseSen; 

    float vertRot = 0f; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Hide and lock cursor here so no one goes crazy 
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // Get mouse movement here
        float mouseX = Input.GetAxis("Mouse X") * mouseSen;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSen;

        // I use clamp here because it gets buggy otherwise
        vertRot -= mouseY;
        vertRot = Mathf.Clamp(vertRot, -90f, 90f);

        transform.localRotation = Quaternion.Euler(vertRot, 0f, 0f);
        player.Rotate(Vector3.up * mouseX);
    }
}
