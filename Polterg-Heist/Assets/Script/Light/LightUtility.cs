using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class LightUtility
{
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

            // Check if any parts of the object is hit by light
            foreach (Vector2 point in samplePoints)
            {
                // Calculate the distance from light to the point position
                Vector2 lightPosition = lightCollider.transform.position;
                float distance = Vector2.Distance(point, lightPosition);
                if (distance <= light.pointLightOuterRadius)
                {
                    // Calculate angle between light's forward direction and the point position
                    Vector2 directionLightToPoint = (point - (Vector2)lightPosition).normalized;
                    float angle = Vector2.Angle(lightCollider.transform.up, directionLightToPoint);

                    // Check if the point is within the outer spot angle
                    if (angle <= light.pointLightOuterAngle / 2)
                    {
                        // Check if walls does not block the light
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

    // Get multiple points across the object collider to see if it's hit by light
    public static Vector2[] GetSamplePointsFromObject(Collider2D objCollider)
    {
        List<Vector2> samplePoints = new List<Vector2>();

        // Add the center
        samplePoints.Add(objCollider.bounds.center);

        // Add the corners
        samplePoints.Add(new Vector2(objCollider.bounds.min.x, objCollider.bounds.min.y)); // Bottom-left
        samplePoints.Add(new Vector2(objCollider.bounds.max.x, objCollider.bounds.max.y)); // Top-right
        samplePoints.Add(new Vector2(objCollider.bounds.min.x, objCollider.bounds.max.y)); // Top-left
        samplePoints.Add(new Vector2(objCollider.bounds.max.x, objCollider.bounds.min.y)); // Bottom-right

        // Add points along the edges
        float width = objCollider.bounds.size.x;
        float height = objCollider.bounds.size.y;

        // Number of additional points to sample along each edge
        int edgeSamples = 10; // You can adjust this value as needed

        // Sample points along the top edge
        for (int i = 1; i < edgeSamples; i++)
        {
            float x = objCollider.bounds.min.x + (width * i / edgeSamples);
            samplePoints.Add(new Vector2(x, objCollider.bounds.max.y));
        }

        // Sample points along the bottom edge
        for (int i = 1; i < edgeSamples; i++)
        {
            float x = objCollider.bounds.min.x + (width * i / edgeSamples);
            samplePoints.Add(new Vector2(x, objCollider.bounds.min.y));
        }

        // Sample points along the left edge
        for (int i = 1; i < edgeSamples; i++)
        {
            float y = objCollider.bounds.min.y + (height * i / edgeSamples);
            samplePoints.Add(new Vector2(objCollider.bounds.min.x, y));
        }

        // Sample points along the right edge
        for (int i = 1; i < edgeSamples; i++)
        {
            float y = objCollider.bounds.min.y + (height * i / edgeSamples);
            samplePoints.Add(new Vector2(objCollider.bounds.max.x, y));
        }

        return samplePoints.ToArray();
        //Vector2[] samplePoints = {
        //                          objCollider.bounds.center, // Center
        //                          (Vector2)objCollider.bounds.min, // Bottom-left
        //                          (Vector2)objCollider.bounds.max, // Top-right
        //                          new Vector2(objCollider.bounds.min.x, objCollider.bounds.max.y), // Top-left
        //                          new Vector2(objCollider.bounds.max.x, objCollider.bounds.min.y) // Bottom-right
        //                          };
        //return samplePoints;
    }

    // Checks if a light is blocked by walls
    public static bool BlockedByWall(Vector2 lightPosition, Vector2 directionLightToObject, float distance, LayerMask wallFloorLayer)
    {
        RaycastHit2D hit = Physics2D.Raycast(lightPosition, directionLightToObject, distance, wallFloorLayer);

        if (hit.collider == null)
        {
            return false;
        }
        return true;
    }
}
