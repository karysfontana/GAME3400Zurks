using UnityEngine;
using UnityEngine.InputSystem; 

public class InspectionMech : MonoBehaviour
{
    [SerializeField] private Transform inspectionPoint; 
    [SerializeField] private float maxDistance = 3.5f;
    public LayerMask interactableLayer;
    public float rotationSpeed = 10f; 
    public AudioSource inspectionSound;

    [Header("Input References")]
    public PlayerInput playerInput; 
    public string gameplayMapName = "Main Controls";
    public string inspectionMapName = "Inspection";
    public InputActionReference interact;
    public InputActionReference cancel;
    public InputActionReference rotate;

    private GameObject inspectedObject;
    public bool IsInspecting { get; private set; } = false;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;

    private InputActionMap gameplayMap;
    private InputActionMap inspectionMap;


    private void OnEnable()
    {
        if (interact != null)
        {
            interact.action.Enable(); 
            interact.action.performed += OnInteractPressed;
        }
        
        if (cancel != null)
        {
            cancel.action.Enable(); 
            cancel.action.performed += OnCancelPressed;
        }
    }

    private void OnDisable()
    {
        interact.action.performed -= OnInteractPressed;
        cancel.action.performed -= OnCancelPressed;
    }

    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        if (playerInput != null && playerInput.currentActionMap.name == inspectionMapName) return;

        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            StartInspection(hit.collider.gameObject);
        }
    }

    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        StopInspection();
    }

    private void StartInspection(GameObject obj)
    {
        inspectedObject = obj; 

        if (inspectionSound != null)
        {
        inspectionSound.Play();
        }

        originalParent = inspectedObject.transform.parent;
        originalPosition = inspectedObject.transform.position;
        originalRotation = inspectedObject.transform.rotation;

        if (obj.TryGetComponent<Renderer>(out var renderer))
        {
            Vector3 meshCenterOffset = obj.transform.position - renderer.bounds.center;
            inspectedObject.transform.SetParent(inspectionPoint);
            inspectedObject.transform.localPosition = Vector3.zero + meshCenterOffset;
            
        }
        else
        {
            inspectedObject.transform.SetParent(inspectionPoint);
            inspectedObject.transform.localPosition = Vector3.zero;
        }
        inspectedObject.transform.localRotation = Quaternion.Inverse(inspectionPoint.rotation) * originalRotation;

        if (inspectedObject.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (rotate != null)
        {
            rotate.action.Reset();
        }

        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap(inspectionMapName);
        }
    }

    private void HandleRotation()
    {
        Vector2 rotationDelta = rotate.action.ReadValue<Vector2>();

        float xRotation = -rotationDelta.x * rotationSpeed * Time.deltaTime;
        float yRotation = rotationDelta.y * rotationSpeed * Time.deltaTime;

        inspectedObject.transform.Rotate(Vector3.up, xRotation, Space.World);
        inspectedObject.transform.Rotate(Vector3.right, yRotation, Space.World);
    }

    private void StopInspection()
    {
        if (inspectedObject == null) return;

        inspectedObject.transform.SetParent(originalParent);
        inspectedObject.transform.position = originalPosition;
        inspectedObject.transform.rotation = originalRotation;
        if (inspectedObject.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
        }

        inspectedObject = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap(gameplayMapName);
        }
    }

    void Update()
    {
        if (playerInput != null && playerInput.currentActionMap.name == inspectionMapName)
        {
            HandleRotation();
        }
    }
}
