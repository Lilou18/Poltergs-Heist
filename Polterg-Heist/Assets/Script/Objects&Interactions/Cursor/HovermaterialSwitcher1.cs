using UnityEngine;

public class HoverMaterialSwitcher1 : MonoBehaviour
{
    // This class manage the change of material when an object is in range and can be possessed
    public Material defaultMaterial;
    public Material hoverMaterial;

    private SpriteRenderer spriteRenderer;
    private PossessionManager possessionManager;

    void Start()
    {
        // Find the SpriteRenderer in children
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        possessionManager = GetComponent<PossessionManager>();

        if (spriteRenderer != null && defaultMaterial != null)
        {
            spriteRenderer.material = defaultMaterial;
        }
    }

    void OnMouseOver()
    {
        if (Time.timeScale == 0) return;
        if (CompareTag("Possess") && spriteRenderer != null)
        {
            if(possessionManager != null && (Vector2.Distance(this.transform.position, possessionManager.Player.transform.position) <= possessionManager.PossessionDistance))
            {
                spriteRenderer.material = hoverMaterial;
            }
            else
            {
                spriteRenderer.material = defaultMaterial;
            }
            
        }
    }

    void OnMouseExit()
    {
        if (Time.timeScale == 0) return;
        if (CompareTag("Possess") && spriteRenderer != null)
        {
            spriteRenderer.material = defaultMaterial;
        }
    }
}
