using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Pulse : MonoBehaviour
{
    public Light2D light2D; // Reference to the 2D light
    public float baseIntensity = 1f;
    public float intensityVariation = 0.3f;
    public float pulseSpeed = 2f;

    void Start()
    {
        if (light2D == null)
        {
            light2D = GetComponent<Light2D>();
        }
    }

    void Update()
    {
        // Pulsating effect using Perlin Noise for more natural flame flicker
        float noise = Mathf.PerlinNoise(Time.time * pulseSpeed, 0f);
        light2D.intensity = baseIntensity + (noise - 0.5f) * intensityVariation * 2f;
    }
}
