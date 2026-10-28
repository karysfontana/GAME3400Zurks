using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Attach to the Player root. Keep this component and PlayerInput enabled.
public class ObjectInteraction : MonoBehaviour
{
    [Serializable]
    public class InteractionTarget
    {
        public string displayName;
        public Transform target;
        [Tooltip("Enable for the bed: invoke its event without entering inspection.")]
        public bool eventOnly;
        [Tooltip("Camera position and rotation while inspecting. Its blue Z axis faces the object.")]
        public Transform focusPoint;
        public UnityEvent onInteract = new UnityEvent();
    }

    [Header("Detection")]
    public Camera playerCamera;
    [Min(0.1f)] public float interactionDistance = 3f;
    [Tooltip("Include walls and furniture so they block interactions through them.")]
    public LayerMask raycastLayers = Physics.DefaultRaycastLayers;
    public InteractionTarget[] targets = new InteractionTarget[0];

    [Header("Inspection")]
    [Tooltip("Drag PlayerControllerNewInputSys here. Do NOT add PlayerInput or this component.")]
    public Behaviour[] controlsToPause = new Behaviour[0];
    [Min(0f)] public float transitionDuration = 0.35f;
    public bool showPrompt = true;

    private InteractionTarget hovered;
    private Transform activeFocus;
    private bool inspecting;
    private bool returning;
    private float elapsed;
    private float nextInteractionTime;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 transitionPosition;
    private Quaternion transitionRotation;
    private Behaviour[] pausedControls;
    private bool[] previousEnabled;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            Debug.LogError("ObjectInteraction: assign the player's Camera.", this);
    }

    private void Update()
    {
        if (playerCamera == null) return;
        hovered = inspecting ? null : FindTarget();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (inspecting)
        {
            if (!returning && (keyboard.eKey.wasPressedThisFrame ||
                              keyboard.escapeKey.wasPressedThisFrame))
                BeginReturn();
            return;
        }

        if (!keyboard.eKey.wasPressedThisFrame || hovered == null ||
            Time.unscaledTime < nextInteractionTime) return;

        InteractionTarget selected = hovered;
        nextInteractionTime = Time.unscaledTime + 0.5f;
        if (selected.eventOnly)
        {
            selected.onInteract?.Invoke();
            return;
        }

        if (selected.focusPoint == null)
        {
            Debug.LogWarning("Assign a Focus Point for " + selected.displayName, this);
            return;
        }
        if (selected.focusPoint == playerCamera.transform ||
            selected.focusPoint.IsChildOf(playerCamera.transform))
        {
            Debug.LogWarning("Focus Point must not be inside the player camera hierarchy.", this);
            return;
        }

        BeginInspection(selected.focusPoint);
        selected.onInteract?.Invoke();
    }

    private InteractionTarget FindTarget()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance,
                             raycastLayers, QueryTriggerInteraction.Ignore)) return null;
        if (targets == null) return null;
        foreach (InteractionTarget item in targets)
        {
            if (item == null || item.target == null) continue;
            if (hit.transform == item.target || hit.transform.IsChildOf(item.target))
                return item;
        }
        return null;
    }

    private void BeginInspection(Transform point)
    {
        Transform cam = playerCamera.transform;
        originalLocalPosition = cam.localPosition;
        originalLocalRotation = cam.localRotation;
        pausedControls = controlsToPause == null
            ? new Behaviour[0] : (Behaviour[])controlsToPause.Clone();
        previousEnabled = new bool[pausedControls.Length];
        // Capture all states before disabling, including duplicate entries.
        for (int i = 0; i < pausedControls.Length; i++)
            if (pausedControls[i] != null) previousEnabled[i] = pausedControls[i].enabled;
        for (int i = 0; i < pausedControls.Length; i++)
        {
            Behaviour control = pausedControls[i];
            if (control != null && control != this && !(control is PlayerInput))
                control.enabled = false;
        }
        activeFocus = point;
        inspecting = true;
        returning = false;
        StartTransition();
    }

    private void StartTransition()
    {
        elapsed = 0f;
        transitionPosition = playerCamera.transform.position;
        transitionRotation = playerCamera.transform.rotation;
    }

    private void BeginReturn()
    {
        returning = true;
        StartTransition();
    }

    private void LateUpdate()
    {
        if (!inspecting || playerCamera == null) return;
        if (!returning && (activeFocus == null || !activeFocus.gameObject.activeInHierarchy))
            BeginReturn();

        Transform cam = playerCamera.transform;
        Transform parent = cam.parent;
        Vector3 endPosition = returning
            ? (parent == null ? originalLocalPosition : parent.TransformPoint(originalLocalPosition))
            : activeFocus.position;
        Quaternion endRotation = returning
            ? (parent == null ? originalLocalRotation : parent.rotation * originalLocalRotation)
            : activeFocus.rotation;

        elapsed += Time.unscaledDeltaTime;
        float t = transitionDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / transitionDuration);
        float smooth = t * t * (3f - 2f * t);
        cam.SetPositionAndRotation(
            Vector3.Lerp(transitionPosition, endPosition, smooth),
            Quaternion.Slerp(transitionRotation, endRotation, smooth));
        if (returning && t >= 1f) RestoreInspection();
    }

    private void RestoreInspection()
    {
        if (!inspecting) return;
        inspecting = false;
        returning = false;
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = originalLocalPosition;
            playerCamera.transform.localRotation = originalLocalRotation;
        }
        if (pausedControls != null)
        {
            for (int i = 0; i < pausedControls.Length; i++)
            {
                Behaviour control = pausedControls[i];
                if (control != null && control != this && !(control is PlayerInput))
                    control.enabled = previousEnabled[i];
            }
        }
        activeFocus = null;
        hovered = null;
        nextInteractionTime = Time.unscaledTime + 0.2f;
    }

    private void OnDisable() { RestoreInspection(); }

    private void OnGUI()
    {
        if (!showPrompt || playerCamera == null) return;
        GUI.Label(new Rect(Screen.width / 2f - 3f, Screen.height / 2f - 10f, 20f, 25f), "+");
        string prompt = inspecting ? "E / Esc - Return" :
            hovered == null ? "" : "E - " + hovered.displayName;
        GUI.Label(new Rect(Screen.width / 2f - 140f, Screen.height - 70f, 400f, 30f), prompt);
    }
}
