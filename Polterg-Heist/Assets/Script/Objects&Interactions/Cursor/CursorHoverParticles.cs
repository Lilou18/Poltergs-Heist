using UnityEngine;

public class CursorHoverParticles : MonoBehaviour
{
    public GameObject hoverParticles; // This contains your ParticleSystem
    private Camera mainCamera;

    private bool wasHovering = false;

    void Start()
    {
        mainCamera = Camera.main;

        if (hoverParticles != null)
            hoverParticles.SetActive(false);

        Cursor.visible = true; // Optional: hide system cursor
    }

    void Update()
    {
        if (Time.timeScale == 0) return;
        // Follow mouse position
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        if (hoverParticles != null)
            hoverParticles.transform.position = mouseWorldPos;

        // Raycast to detect 'Possess' tag
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        bool isHovering = hit.collider != null && hit.collider.CompareTag("Possess");

        if (isHovering && !wasHovering)
        {
            hoverParticles?.SetActive(true);
        }
        else if (!isHovering && wasHovering)
        {
            hoverParticles?.SetActive(false);
        }

        wasHovering = isHovering;
    }
}
