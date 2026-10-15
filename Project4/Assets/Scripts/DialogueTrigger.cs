using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public DialogueLine[] dialogueLines;

    public bool dialogueStarted = false;
    private bool playerInit = false;

    private bool hasPlayed = false;

    public void CallStart()
    {
        if (hasPlayed)
        {
            return;
        }

        if (PhoneInteraction.isPickedUp)
        {
            hasPlayed = true;
            dialogueStarted = true;

            DialogueManager.instance.StartDia(dialogueLines);
        }
    }

    public void OnPlayerInit()
    {
        if (PhoneInteraction.isPickedUp)
        {
            playerInit = true;
        }
    }

    private void OnPlayerDown()
    {
        if (!PhoneInteraction.isPickedUp)
        {
            playerInit = false;
        }
    }
}
