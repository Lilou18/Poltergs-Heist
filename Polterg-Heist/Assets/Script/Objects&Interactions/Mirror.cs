using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Mirror : MonoBehaviour
{
    // Manages the mirror reflection system.
    // Uses a secondary camera to determine what is reflected in the mirror.
    // Provides methods to check if an object is reflected and if the reflection is visible to an NPC.

    [SerializeField] private Camera mirrorCamera;       // Orthographic camera positioned to capture the mirror's reflection
    [SerializeField] private LayerMask ignoreLayer;     // Layers ignored when raycasting to check if the reflection is blocked
    private Collider2D mirrorCollider;
    private void Start()
    {
        mirrorCollider = GetComponent<Collider2D>();
    }

    // Activates the mirror camera only when the mirror is visible to the main camera.
    // This avoids rendering the reflection when it's not needed.
    // For performance purposes.
    private void OnBecameVisible()
    {

        mirrorCamera.gameObject.SetActive(true);
    }

    // Deactivate the mirror camera when the mirror is not visible to the main camera.
    // For performance purposes.
    private void OnBecameInvisible()
    {
        mirrorCamera.gameObject.SetActive(false);
    }

    // Returns true if any sampled point of the given collider appears
    // within the mirror camera's viewport which meaning the object is reflected.
    public bool IsReflectedInMirror(Collider2D objCollider)
    {
        if (objCollider.enabled == false)
        {
            return false;
        }

        return GetReflectionPoints(objCollider).Length > 0;
    }

    // Returns the world-space positions of sampled points on the collider
    // as they appear in the mirror's reflection.
    // Only points that fall within the mirror camera's viewport are included.
    public Vector2[] GetReflectionPoints(Collider2D objCollider)
    {
        // Sample points from the object collider
        Vector2[] objectPoints = LightUtility.GetSamplePointsFromObject(objCollider);

        List<Vector2> reflectedPoints = new List<Vector2>();

        foreach (Vector2 point in objectPoints)
        {
            Vector3 reflectionPoint = mirrorCamera.WorldToViewportPoint(point);

            // Only include points that are within the mirror camera's view
            if (reflectionPoint.x >= 0 && reflectionPoint.x <= 1 &&
               reflectionPoint.y >= 0 && reflectionPoint.y <= 1 &&
               reflectionPoint.z >= 0)
            {
                // Convert back to world space using the mirror camera's near clip plane
                Vector3 worldPoint = mirrorCamera.ViewportToWorldPoint(
                    new Vector3(reflectionPoint.x, reflectionPoint.y, mirrorCamera.nearClipPlane));
                reflectedPoints.Add(worldPoint);
            }
        }
        return reflectedPoints.ToArray();
    }

    // Returns true if all rays from the mirror surface to the reflected player points are obstructed.
    // Returns false if at least one ray reaches the player unobstructed because the reflection is visible.
    public bool IsMirrorReflectionBlocked(Vector2[] reflectedPoints, Collider2D playerCollider)
    {
        if (playerCollider == null) return true;

        // Cast rays from multiple points on the mirror to various points on the player's collider
        Vector2[] mirrorPoints = LightUtility.GetSamplePointsFromObject(mirrorCollider);

        // Check if any ray from the mirror to the player is unobstructed
        foreach (Vector2 mirrorPoint in mirrorPoints)
        {
            foreach (Vector2 playerPoint in reflectedPoints)
            {
                Vector2 direction = (playerPoint - mirrorPoint).normalized;
                float distance = Vector2.Distance(mirrorPoint, playerPoint);

                // Cast ray from mirror point to player point
                RaycastHit2D hit = Physics2D.Raycast(mirrorPoint, direction, distance, ~ignoreLayer);

                // At least one unobstructed ray found the player. The reflection is visible
                if (hit.collider == playerCollider)
                {
                    return false;
                }
            }
        }

        // All rays were obstructed
        return true;
    }
}
