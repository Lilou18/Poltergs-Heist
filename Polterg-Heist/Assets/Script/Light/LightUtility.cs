using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class LightUtility
{
    // Utility class for light-based visibility checks.
    // Used by HumanNPCBehaviour to determine if objects are illuminated

    // Returns true if the given light collider illuminates any part of the target collider.
    // Only Point lights are supported. Global and other light types are ignored because
    // we don't use them.
    // Walls and floors can block the light ray.
    public static bool IsPointHitByLight(Collider2D lightCollider, Collider2D npcCollider, LayerMask wallFloorLayer)
    {
        Light2D light = lightCollider.GetComponent<Light2D>();

        if (light == null || !light.enabled)
            return false;

        // We ignore global lights
        if (light.lightType == Light2D.LightType.Global)
            return false;

        if(light.lightType == Light2D.LightType.Point)
        {
            Vector2[] samplePoints = GetSamplePointsFromObject(npcCollider);
            Vector2 lightPosition = lightCollider.transform.position;

            // Check if any parts of the object is hit by light
            foreach (Vector2 point in samplePoints)
            {
                // Calculate the distance from light to the point position                
                float distance = Vector2.Distance(point, lightPosition);

                // Skip points outside the light's outer radius
                if (distance <= light.pointLightOuterRadius)
                {
                    // Check if the point is within the light's cone angle
                    Vector2 directionLightToPoint = (point - (Vector2)lightPosition).normalized;
                    float angle = Vector2.Angle(lightCollider.transform.up, directionLightToPoint);
             
                    if (angle <= light.pointLightOuterAngle / 2)
                    {
                        // Check if a wall or floor blocks the light ray
                        if (!BlockedByWall(lightPosition, directionLightToPoint, distance, wallFloorLayer))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        
        return false;
    }

    // Samples multiple points distributed across the object's bounding box.
    // Used to approximate whether any part of the object is illuminated or visible.
    // Includes center, corners, and evenly spaced points along each edge.
    public static Vector2[] GetSamplePointsFromObject(Collider2D objCollider)
    {
        List<Vector2> samplePoints = new List<Vector2>();

        Bounds objColliderBounds = objCollider.bounds;

        // Center
        samplePoints.Add(objColliderBounds.center);

        // Corners
        samplePoints.Add(new Vector2(objColliderBounds.min.x, objColliderBounds.min.y)); // Bottom-left
        samplePoints.Add(new Vector2(objColliderBounds.max.x, objColliderBounds.max.y)); // Top-right
        samplePoints.Add(new Vector2(objColliderBounds.min.x, objColliderBounds.max.y)); // Top-left
        samplePoints.Add(new Vector2(objColliderBounds.max.x, objColliderBounds.min.y)); // Bottom-right

        // Edge samples — higher count increases accuracy at the cost of performance
        int edgeSamples = 3; // You can adjust this value as needed

        // Add points along the edges of the collider
        for (int i = 1; i < edgeSamples; i++)
        {
            float x = objColliderBounds.min.x + objColliderBounds.size.x * i / edgeSamples;
            float y = objColliderBounds.min.y + objColliderBounds.size.y * i / edgeSamples;

            samplePoints.Add(new Vector2(x, objColliderBounds.max.y)); // Top edge
            samplePoints.Add(new Vector2(x, objColliderBounds.min.y)); // Bottom edge
            samplePoints.Add(new Vector2(objColliderBounds.min.x, y)); // Left edge
            samplePoints.Add(new Vector2(objColliderBounds.max.x, y)); // Right edge
        }
        return samplePoints.ToArray();
    }

    // Returns true if a wall or floor collider blocks the ray from the light to the target point.
    public static bool BlockedByWall(Vector2 lightPosition, Vector2 directionLightToObject, float distance, LayerMask wallFloorLayer)
    {
        RaycastHit2D hit = Physics2D.Raycast(lightPosition, directionLightToObject, distance, wallFloorLayer);

        return hit.collider != null;
    }
}
