using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlickering : MonoBehaviour
{
    [Header("Light variables")]
    [SerializeField] float delayBeforeLightOn;  // Delay before the light turns on
    [SerializeField] float lightDuration;   // Length of time the light is on
    [SerializeField] float lightOffDuration;    // Length of time the light is off
    Light2D lightSource;

    private void Start()
    {
        lightSource = GetComponent<Light2D>();
        lightSource.enabled = false;
        StartCoroutine(WaitingStart());
    }

    private void Update()
    {
        
    }

    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(WaitingStart());
    }

    private IEnumerator WaitingStart()
    {
        yield return new WaitForSeconds(delayBeforeLightOn);
        StartCoroutine(ToggleLightOn());
    }

    private IEnumerator ToggleLightOn()
    {
        lightSource.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        StartCoroutine(ToggleLightOff());
    }
    
    private IEnumerator ToggleLightOff()
    {
        lightSource.enabled = false;
        yield return new WaitForSeconds(lightOffDuration);
        StartCoroutine(ToggleLightOn());
    }


}
