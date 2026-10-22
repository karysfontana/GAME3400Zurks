using UnityEngine;
using System.Collections;

public class ClockTick : MonoBehaviour
{
    public GameObject secondNeedlePivot;
    public GameObject minuteNeedlePivot;
    public GameObject hourNeedlePivot;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SecondHand());
    }

    // Update is called once per frame
    IEnumerator SecondHand()
    {
        while (true)
        {

            yield return new WaitForSeconds(1.0f);
            secondNeedlePivot.transform.Rotate(0, 0, 6f);
            minuteNeedlePivot.transform.Rotate(0, 0, .1f);
            hourNeedlePivot.transform.Rotate(0, 0, .1f / 12);

        }

    }

}
