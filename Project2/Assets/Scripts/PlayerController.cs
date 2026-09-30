using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.Scripting.APIUpdating;

/* 
   This was based off of an old script I made for previous projects, but updated for the new input system.
   I updated it because of the r/unity subreddit which is linked in the design doc. I used Unity's docs to 
   update the old input system with the new-the math was mostly the same but for the FPS camera I used 
   another old scripts math. 
*/

/*
    This is a controller script using the new unity input system. Finish documentation later idk.
*/

public class PlayerController : MonoBehaviour
{

    [Header("Movement")]
    [SerializeField] private float speed = 5f; 
    [SerializeField] private float jumpHeight = 5; 
    [SerializeField] private float gravity = -9.8f; 
    [SerializeField] private float sprintMod = 1.5f; 

    [Header("Cam")]
    // PUT MAIN CAMERA HERE 
    [SerializeField] private Transform cameraTransform; 
    [SerializeField] private float mouseSensitivity = 10f; 
    [SerializeField] private float lookLimit = 80f; 

    Vector3 moveInput; 
    Vector2 lookInput;
    Vector3 velocity; 
    CharacterController controller;
    float verticalRotation = 0f;  
    bool isSprinting = false; 
    bool canMove = true;
    
    void Start()
    {
        controller = GetComponent<CharacterController>(); 

        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;

        // In case assignment isn't done in inspector
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }


    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>(); 
        Debug.Log($"Move input: " + moveInput); 
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && controller.isGrounded)
        {
            Debug.Log($"Should jump"); 
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); 
        }
    }

    // Sprint function (new to script 9/29)
    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log($"Sprinting"); 
            isSprinting = true; 
        }
        else if (context.canceled)
        {
            Debug.Log($"Walking");
            isSprinting = false; 
        }
    }

    // For first person perspective 
    public void Look(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        if (!canMove)
        {
        return;
        }

        float currentSpeed = isSprinting ? speed * sprintMod : speed;
        // Mouse input 
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // Rotate player horizontally & camera vertically 
        transform.Rotate(Vector3.up * mouseX);
        verticalRotation -= mouseY; 
        verticalRotation = Mathf.Clamp(verticalRotation, -lookLimit, lookLimit);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f); 

        // Move Player & Cam
        // If not FPS, uncomment below line out and comment FPS line
        // Vector3 move = new Vector3(moveInput.x, 0, moveInput.y); 
        Vector3 move = (transform.forward * moveInput.y) + (transform.right * moveInput.x); 
        controller.Move(move * currentSpeed * Time.deltaTime);
        
        // Jump
        velocity.y += gravity * Time.deltaTime; 
        controller.Move(velocity * Time.deltaTime); 

        // If not FPS, add in camera movement logic tho it would 
        // probs be best to put that in its own camera script. 
    }

    public void StopPlayer()
    {
        canMove = false;
        moveInput = Vector3.zero;
        velocity = Vector3.zero;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Respawn(Vector3 position)
    {
        controller.enabled = false;

        transform.position = position;

        velocity = Vector3.zero;
        moveInput = Vector3.zero;

        controller.enabled = true;

        canMove = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}