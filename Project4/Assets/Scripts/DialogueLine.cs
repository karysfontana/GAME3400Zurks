using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speaker; 
    [TextArea(1, 3)]
    public string dialogueText;
}
