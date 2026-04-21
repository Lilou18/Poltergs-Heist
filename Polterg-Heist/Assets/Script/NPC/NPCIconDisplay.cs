using UnityEngine;
using UnityEngine.Rendering.Universal;

public class NPCIconDisplay : MonoBehaviour
{

    [SerializeField] private SpriteRenderer iconRenderer;       // Renderer used to display the icon above the NPC
    private Light2D fovLight;                                   // Light reprensenting the FOV of the NPC

    [Header("Icons")]
    [SerializeField] protected Sprite investigationIcon;        // Icon displayed when NPC is investigating
    [SerializeField] protected Sprite alertIcon;                // Icon displayed when NPC is alerted

    [Header("FOV Colors")]
    [SerializeField] protected Color nonSuspiciousColorFOV;     // Default FOV color when NPC is non suspicious
    [SerializeField] protected Color alertColorFOV;             // FOV color when NPC is alerted

    private void Awake()
    {
        fovLight = GetComponentInChildren<Light2D>();
    }

    // Change the icon and the FOV light color base on the given state.
    public void UpdateIcon(IconState state)
    {
        if (iconRenderer == null || fovLight == null) return;

        switch (state)
        {
            case IconState.None:
                iconRenderer.enabled = false;
                fovLight.color = nonSuspiciousColorFOV;
                break;
            case IconState.Alert:
                iconRenderer.sprite = alertIcon;
                iconRenderer.enabled = true;
                fovLight.color = alertColorFOV;
                break;
            case IconState.Investigation:
                iconRenderer.sprite = investigationIcon;
                iconRenderer.enabled = true;
                fovLight.color = nonSuspiciousColorFOV;
                break;
        }
    }

    // Resets the icon display and the FOV light to its default state.
    public void Reset()
    {
        if (iconRenderer != null)
            iconRenderer.enabled = false;
        if (fovLight != null)
            fovLight.color = nonSuspiciousColorFOV;
    }
}

public enum IconState
{
    None,
    Investigation,
    Alert
}
