using UnityEngine;
using UnityEngine.UI;
using TMPro; 

/**
   This script is for the name of inspectable objects to be displayed
   to the player when they look at it. It also additional text to inform 
   players how to inspect the object. MAKE SURE inspectable objects have 
   sensical names. 
   **/

public class DisplayNameUi : MonoBehaviour
{
    [SerializeField] private TMP_Text objectNameText; 
    [SerializeField] private TMP_Text inspectionTutText; 

    [SerializeField] private LayerMask interactableLayer; 
    [SerializeField] private float maxDistance = 10f;

    // Update is called once per frame
    void Update()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        Ray ray = Camera.main.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out var hit, maxDistance, interactableLayer))
        {
            objectNameText.text = hit.collider.gameObject.name;
            inspectionTutText.enabled = true;
        }
        else
        {
            objectNameText.text = "";
            inspectionTutText.enabled = false;
        }
    }
}
