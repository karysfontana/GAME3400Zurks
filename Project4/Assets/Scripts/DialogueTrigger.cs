using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public DialogueLine[] dialogueLines;

    [Header("Audio Link")]
    [SerializeField] private bool playsOnAwake; 
    [SerializeField] private AudioSource sfxSource; 
    [SerializeField] private AudioClip hangUpSfx; 
    [SerializeField] private AudioClip ringSfx; 

    //public DialogueLine[] dialogueLines;
    
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
            StopOrPlay(); 
        }
    }

    public void Update()
    {
        if (DialogueManager.instance.dialogueFinished == true)
        {
            if (sfxSource)
            {
                sfxSource.clip = hangUpSfx;
                sfxSource.Play();
            }
        }
    }

    public void CallEnd()
    {
        if (sfxSource)
        {
            sfxSource.clip = hangUpSfx;
            sfxSource.Play();
        }
    }

    private void StopOrPlay()
    {
        if (sfxSource && playsOnAwake == true)
        {
            sfxSource.Stop(); 
        }

        // for 2020s scene start dial tone on pick up 
        if (sfxSource && playsOnAwake == false)
        {
            sfxSource.clip = hangUpSfx;
            sfxSource.Play();
        }
    }
    private void OnPlayerInit()
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
