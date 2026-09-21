using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

/* This is a script I have from old projects for a basic 
    player controller
*/
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f; 
    [SerializeField] private float jumpForce = 5; 

    Rigidbody rb;
    bool isGrounded;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Jump();
    }

    void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Debug.Log("Horizontal: " + horizontal);
        Debug.Log("Vectical: " + vertical);

        // Compute movement vector.
        Vector3 movement = new Vector3(horizontal, 0, vertical).normalized;

        // Apply force.
        rb.AddForce(movement * speed);
    }

    void Jump()
    {
        if(Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            // disable jump
            isGrounded = false; 
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collided with " + collision.transform.name);

        // Assume in list of contacts gound is always the first. 
        ContactPoint contact = collision.contacts[0];

        if(contact.normal.y > 0.5f)
        {
            isGrounded = true; 
        }

        Debug.Log("Contact position:  " + contact.point);
        Debug.Log("Contact normal:  " + contact.normal);

    }
}