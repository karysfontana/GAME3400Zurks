using UnityEngine;
using UnityEngine.UI; 
using TMPro; 
using System.Collections; 
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    // Make it a Singleton so we can access this from anywhere thanks bye 
    public static DialogueManager instance; 

    [Header("Linked Components")]
    public TextMeshProUGUI nameBox; 
    public TextMeshProUGUI textBox; 
    public GameObject dialogueGameObj; 
    public AudioSource sfxSource; 
    public AudioClip hangUpSfx; 

    [Header("Text Config")]
    public float typingSpeed = 0.05f; 

    [Header("Dialogue Status")]
    public bool isTyping = false; 
    public bool dialogueFinished = true; 

    [Header("Dialogue")]
    public DialogueLine[] dialogueLines; 

    private int currentInd = 0; 
    private Coroutine typingCo; 
    private bool justStarted = false; 

    private void Awake()
    {
        if(instance == null)
        {
            instance = this; 
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    /** 
        Function to start up dialogue. Must be public bc it will be called from another script. 
    **/ 

public void StartDia(DialogueLine[] newLines)
{
    if (newLines == null || newLines.Length == 0)
    {
        Debug.LogWarning("No dialogue lines assigned!");
        return;
    }

    currentInd = 0;
    isTyping = false;

    dialogueGameObj.SetActive(true);
    dialogueFinished = false;
    dialogueLines = newLines;
    justStarted = true;
    if (sfxSource)
    {
        sfxSource.Stop(); 
    }

    typingCo = StartCoroutine(TypeLine(dialogueLines[currentInd]));
}
    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (justStarted)
            {
                justStarted = false; 
                return;
            }

            // Skip typing 
            if (isTyping)
            {
                StopCoroutine(typingCo);
                ShowLine(dialogueLines[currentInd]); 
                isTyping = false; 
            }
            // if isTyping is already false go to next line or end dialogue. 
            else
            {
                currentInd++; 

                if (currentInd < dialogueLines.Length)
                {
                    typingCo = StartCoroutine(TypeLine(dialogueLines[currentInd])); 
                }
                // End dialogue
                else
                {
                    textBox.text = ""; 
                    if (nameBox)
                    {
                        nameBox.text = ""; 
                    }
                    dialogueFinished = true; 
                    if (sfxSource != null && hangUpSfx != null)
                    {
                        sfxSource.clip = hangUpSfx;
                        sfxSource.Play();
                    }
                    dialogueGameObj.SetActive(false); 
                }
            }
        }
    }

    /**
        Co-routine to type out text by line. If it's too slow change typingSpeed var.
    **/ 
    IEnumerator TypeLine (DialogueLine line)
    {
        isTyping = true; 

        textBox.text = ""; 
        // If adding a name for speaker ig
        if (nameBox)
        {
            nameBox.text = line.speaker; 
        }

        // actually type text
        foreach (char c in line.dialogueText)
        {
            textBox.text += c; 
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false; 
    }

    /**
        Function to skip typing co-routine bc I'm impatient. 
    **/ 
    private void ShowLine (DialogueLine line)
    {
        textBox.text = line.dialogueText;
        // If adding a name for speaker ig
        if (nameBox)
        {
            nameBox.text = line.speaker; 
        }
    }
}
