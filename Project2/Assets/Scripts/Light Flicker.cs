using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    private Light flickerLight;

    public float minIntensity = 0f;
    public float maxIntensity = 10f;
    public float minWaitTime = 0.05f;
    public float maxWaitTime = 0.3f;

    void Start()
    {
        flickerLight = GetComponent<Light>();
        Invoke("Flicker", Random.Range(minWaitTime, maxWaitTime));
    }

    void Flicker()
    {
        flickerLight.intensity =
            Random.Range(minIntensity, maxIntensity);

        Invoke("Flicker", Random.Range(minWaitTime, maxWaitTime));
    }
}
