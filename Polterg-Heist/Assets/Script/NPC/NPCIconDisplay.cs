using UnityEngine;
using UnityEngine.Rendering.Universal;

public class NPCIconDisplay : MonoBehaviour
{

    [SerializeField] private SpriteRenderer iconRenderer;
    private Light2D fovLight;

    [SerializeField] protected Sprite investigationIcon;
    [SerializeField] protected Sprite alertIcon;

    [SerializeField] protected Color nonSuspiciousColorFOV;
    [SerializeField] protected Color alertColorFOV;

    private void Awake()
    {
        fovLight = GetComponentInChildren<Light2D>();
    }

    public void UpdateIcon(IconState state)
    {
        if (iconRenderer == null) return;

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
