using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlickering : MonoBehaviour
{
    // Handles a looping flickering light behavior.
    // The light alternates between ON and OFF states using configurable durations.
    
    [Header("Light Timing")]
    [SerializeField] float delayBeforeLightOn;  // Initial delay before the flickering starts
    [SerializeField] float lightDuration;       // Duration the light stays On
    [SerializeField] float lightOffDuration;    // Duration the light stays Off
    Light2D lightSource;

    private Coroutine flickerRoutine;           // FlickeringLightLoop coroutine reference

    private void Awake()
    {
        lightSource = GetComponent<Light2D>();
    }

    // Called when the object becomes enabled.
    // Starts the flickering loop.
    private void OnEnable()
    {
        if (lightSource == null) return;

        lightSource.enabled = false;
        flickerRoutine = StartCoroutine(FlickeringLightLoop());
    }

    // Prevent orphan coroutine.
    private void OnDisable()
    {
        if(flickerRoutine != null)
            StopCoroutine(flickerRoutine);
    }

    // Behaviour for the flickering flight on a loop.
    // 1. Wait initial delay
    // 2. Turn the light On, then wait
    // 3. Turn the light Off, then wait
    private IEnumerator FlickeringLightLoop()
    {
        yield return new WaitForSeconds(delayBeforeLightOn);

        while (true)
        {
            // Turn On the Light
            lightSource.enabled = true;
            yield return new WaitForSeconds(lightDuration);

            // Turn Off the light
            lightSource.enabled = false;
            yield return new WaitForSeconds(lightOffDuration);
        }
    }
}
