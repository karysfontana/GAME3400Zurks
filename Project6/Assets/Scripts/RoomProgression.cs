using System;
using UnityEngine;
using UnityEngine.Events;

// Attach to an always-active RoomManager, outside all stage roots.
public class RoomProgression : MonoBehaviour
{
    [Serializable]
    public class RoomStage
    {
        public string stageName;
        public GameObject objects;
        public UnityEvent onEnter = new UnityEvent();
    }

    public RoomStage[] stages = new RoomStage[0];
    [Min(0)] public int startingStage = 0;
    public int CurrentStage { get; private set; }
    private bool initialized;
    private bool applying;

    private void Start() { Initialize(); }

    private bool Initialize()
    {
        if (initialized) return true;
        if (stages == null || stages.Length == 0)
        {
            Debug.LogWarning("RoomProgression: add room stages in the Inspector.", this);
            return false;
        }
        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i] == null || stages[i].objects == null)
            {
                Debug.LogError("RoomProgression: assign Objects for stage " + i, this);
                return false;
            }
            Transform root = stages[i].objects.transform;
            if (transform == root || transform.IsChildOf(root))
            {
                Debug.LogError("RoomManager must be outside stage objects.", this);
                return false;
            }
            for (int j = 0; j < i; j++)
            {
                Transform other = stages[j].objects.transform;
                if (root == other || root.IsChildOf(other) || other.IsChildOf(root))
                {
                    Debug.LogError("Stage roots must be separate, non-nested objects.", this);
                    return false;
                }
            }
        }
        CurrentStage = Mathf.Clamp(startingStage, 0, stages.Length - 1);
        initialized = true;
        ApplyStage();
        return true;
    }

    public void NextStage()
    {
        if (applying || !Initialize()) return;
        if (CurrentStage >= stages.Length - 1) return;
        CurrentStage++;
        ApplyStage();
    }

    public void RestartProgression()
    {
        if (applying || !Initialize()) return;
        CurrentStage = Mathf.Clamp(startingStage, 0, stages.Length - 1);
        ApplyStage();
    }

    private void ApplyStage()
    {
        applying = true;
        try
        {
            // Hide previous visuals before showing the next set.
            for (int i = 0; i < stages.Length; i++)
                if (i != CurrentStage && stages[i].objects != null)
                    stages[i].objects.SetActive(false);
            if (stages[CurrentStage].objects != null)
                stages[CurrentStage].objects.SetActive(true);
            Debug.Log("Room stage " + (CurrentStage + 1) + ": " +
                      stages[CurrentStage].stageName, this);
            stages[CurrentStage].onEnter?.Invoke();
        }
        finally { applying = false; }
    }
}
