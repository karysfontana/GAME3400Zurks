using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ObjectInteraction : MonoBehaviour
{
    [Serializable]
    public class InteractionTarget
    {
        public string displayName;
        public Transform target;
        public bool eventOnly;
        public Transform focusPoint;
        public UnityEvent onInteract = new UnityEvent();
    }

    [Header("Detection")]
    public Camera playerCamera;
    [Min(0.1f)] public float interactionDistance = 3f;
    public LayerMask raycastLayers = Physics.DefaultRaycastLayers;
    public InteractionTarget[] targets = new InteractionTarget[0];

    [Header("Inspection")]
    public Behaviour[] controlsToPause = new Behaviour[0];
    [Min(0f)] public float transitionDuration = 0.35f;
    public bool showPrompt = true;

    private InteractionTarget hovered;
    private Transform focus;
    private bool inspecting;
    private bool returning;
    private float elapsed;
    private float nextInteractionTime;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Behaviour[] pausedControls;
    private bool[] wasEnabled;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null)
            Debug.LogError("Assign the player's Camera.", this);
    }

    private void Update()
    {
        if (playerCamera == null) return;

        if (!inspecting)
            hovered = FindTarget();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool pressedE = keyboard.eKey.wasPressedThisFrame;
        if (inspecting)
        {
            if (!returning && (pressedE || keyboard.escapeKey.wasPressedThisFrame))
                BeginReturn();
            return;
        }

        if (!pressedE || hovered == null || Time.unscaledTime < nextInteractionTime)
            return;

        InteractionTarget selected = hovered;
        nextInteractionTime = Time.unscaledTime + 0.5f;

        if (!selected.eventOnly)
        {
            Transform point = selected.focusPoint;
            if (point == null)
            {
                Debug.LogWarning("Assign a Focus Point for " + selected.displayName, this);
                return;
            }

            Transform cam = playerCamera.transform;
            if (point == cam || point.IsChildOf(cam))
            {
                Debug.LogWarning("Move the Focus Point outside the camera hierarchy.", this);
                return;
            }

            BeginInspection(point);
        }

        if (selected.onInteract != null)
            selected.onInteract.Invoke();
    }

    private InteractionTarget FindTarget()
    {
        if (targets == null) return null;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, interactionDistance,
                            raycastLayers, QueryTriggerInteraction.Ignore))
            return null;

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
        originalPosition = cam.localPosition;
        originalRotation = cam.localRotation;

        pausedControls = new Behaviour[0];
        if (controlsToPause != null)
            pausedControls = (Behaviour[])controlsToPause.Clone();

        wasEnabled = new bool[pausedControls.Length];

        for (int i = 0; i < pausedControls.Length; i++)
        {
            if (pausedControls[i] != null)
                wasEnabled[i] = pausedControls[i].enabled;
        }

        for (int i = 0; i < pausedControls.Length; i++)
        {
            Behaviour control = pausedControls[i];
            if (control != null && control != this && !(control is PlayerInput))
                control.enabled = false;
        }

        focus = point;
        inspecting = true;
        returning = false;
        StartTransition();
    }

    private void StartTransition()
    {
        elapsed = 0f;
        startPosition = playerCamera.transform.position;
        startRotation = playerCamera.transform.rotation;
    }

    private void BeginReturn()
    {
        returning = true;
        StartTransition();
    }

    private void LateUpdate()
    {
        if (!inspecting || playerCamera == null) return;

        if (!returning && (focus == null || !focus.gameObject.activeInHierarchy))
            BeginReturn();

        Transform cam = playerCamera.transform;
        Vector3 endPosition;
        Quaternion endRotation;

        if (returning)
        {
            endPosition = originalPosition;
            endRotation = originalRotation;
            if (cam.parent != null)
            {
                endPosition = cam.parent.TransformPoint(originalPosition);
                endRotation = cam.parent.rotation * originalRotation;
            }
        }
        else
        {
            endPosition = focus.position;
            endRotation = focus.rotation;
        }

        elapsed += Time.unscaledDeltaTime;
        float progress = 1f;
        if (transitionDuration > 0f)
            progress = Mathf.Clamp01(elapsed / transitionDuration);

        float smooth = Mathf.SmoothStep(0f, 1f, progress);
        cam.SetPositionAndRotation(
            Vector3.Lerp(startPosition, endPosition, smooth),
            Quaternion.Slerp(startRotation, endRotation, smooth));

        if (returning && progress >= 1f)
            RestoreInspection();
    }

    private void RestoreInspection()
    {
        if (!inspecting) return;

        inspecting = false;
        returning = false;

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = originalPosition;
            playerCamera.transform.localRotation = originalRotation;
        }

        for (int i = 0; i < pausedControls.Length; i++)
        {
            Behaviour control = pausedControls[i];
            if (control != null && control != this && !(control is PlayerInput))
                control.enabled = wasEnabled[i];
        }

        focus = null;
        hovered = null;
        nextInteractionTime = Time.unscaledTime + 0.2f;
    }

    private void OnDisable()
    {
        RestoreInspection();
    }

    private void OnGUI()
    {
        if (!showPrompt || playerCamera == null) return;

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;
        GUI.Label(new Rect(centerX - 3f, centerY - 10f, 20f, 25f), "+");

        string prompt = "";
        if (inspecting)
            prompt = "E / Esc - Return";
        else if (hovered != null)
            prompt = "E - " + hovered.displayName;

        GUI.Label(new Rect(centerX - 140f, Screen.height - 70f, 400f, 30f), prompt);
    }
}
