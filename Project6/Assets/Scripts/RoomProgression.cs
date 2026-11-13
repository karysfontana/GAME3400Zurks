using System;
using UnityEngine;
using UnityEngine.Events;

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

    [Min(0)]
    public int startingStage = 0;

    public int CurrentStage { get; private set; }

    private bool initialized;
    private bool applying;

    private void Start()
    {
        Initialize();
    }

    private bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        if (stages == null || stages.Length == 0)
        {
            Debug.LogWarning("RoomProgression: add room stages in the Inspector.", this);
            return false;
        }

        for (int i = 0; i < stages.Length; i++)
        {
            RoomStage stage = stages[i];

            if (stage == null || stage.objects == null)
            {
                Debug.LogError("RoomProgression: assign Objects for stage " + i, this);
                return false;
            }

            Transform stageRoot = stage.objects.transform;

            if (transform == stageRoot || transform.IsChildOf(stageRoot))
            {
                Debug.LogError("RoomManager must be outside stage objects.", this);
                return false;
            }

            for (int j = 0; j < i; j++)
            {
                Transform otherRoot = stages[j].objects.transform;

                if (stageRoot == otherRoot ||
                    stageRoot.IsChildOf(otherRoot) ||
                    otherRoot.IsChildOf(stageRoot))
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
        if (applying)
        {
            return;
        }

        if (!Initialize())
        {
            return;
        }

        if (CurrentStage >= stages.Length - 1)
        {
            return;
        }

        CurrentStage++;
        ApplyStage();
    }

    public void RestartProgression()
    {
        if (applying)
        {
            return;
        }

        if (!Initialize())
        {
            return;
        }

        CurrentStage = Mathf.Clamp(startingStage, 0, stages.Length - 1);
        ApplyStage();
    }

    private void ApplyStage()
    {
        applying = true;

        try
        {
            for (int i = 0; i < stages.Length; i++)
            {
                if (i != CurrentStage && stages[i].objects != null)
                {
                    stages[i].objects.SetActive(false);
                }
            }

            if (stages[CurrentStage].objects != null)
            {
                stages[CurrentStage].objects.SetActive(true);
            }

            RoomStage current = stages[CurrentStage];
            int stageNumber = CurrentStage + 1;
            Debug.Log("Room stage " + stageNumber + ": " + current.stageName, this);

            if (current.onEnter != null)
            {
                current.onEnter.Invoke();
            }
        }
        finally
        {
            applying = false;
        }
    }
}
