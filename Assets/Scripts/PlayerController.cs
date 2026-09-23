using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.Scripting.APIUpdating;

/* This was based off of an old script I made for previous projects, but updated for the new input system 
   and with no jump logic. 
*/
// [RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f; 
    [SerializeField] private float jumpHeight = 5; 
    [SerializeField] private float gravity = -9.8f; 

    Rigidbody rb;
    Vector3 moveInput; 
    Vector3 velocity; 
    CharacterController controller; 
    
    void Start()
    {
        controller = GetComponent<CharacterController>(); 
        rb = GetComponent<Rigidbody>();
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
    // Moved this into Update() so it calls more frequently, makes movement smoother
    void Update()
    {
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y); 
        controller.Move(move * speed * Time.deltaTime); 

        velocity.y += gravity * Time.deltaTime; 
        controller.Move(velocity * Time.deltaTime); 

    }
}