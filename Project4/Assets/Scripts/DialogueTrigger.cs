using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public DialogueLine[] dialogueLines; 

    public bool dialogueStarted = false; 
    private bool playerInit = false; 

    public void CallStart()
    {
        OnPlayerInit(); 
        // hoping this works lol 
        if (playerInit == true && dialogueStarted == false)
        {
            DialogueManager.instance.StartDia(dialogueLines);
            dialogueStarted = true; 
        }

        if (dialogueStarted && DialogueManager.instance.dialogueFinished == true)
        {
            dialogueStarted = false; 
        }
    }

    public void OnPlayerInit()
    {
        if (PhoneInteraction.isPickedUp == true)
        {
            playerInit = true; 
        }
    }

    private void OnPlayerDown()
    {
        if (PhoneInteraction.isPickedUp == false)
        {
            playerInit = false; 
        }
    }
}
