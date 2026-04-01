using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Mirror : MonoBehaviour
{
    [SerializeField] private Camera mirrorCamera;
    private float reflectionDistance;   // Distance between the mirror and it's reflection
    private Plane[] planes;
    [SerializeField] private LayerMask ignoreLayer;
    private MeshRenderer m_Renderer;
    private void Start()
    {
        planes = GeometryUtility.CalculateFrustumPlanes(mirrorCamera);
        reflectionDistance = mirrorCamera.orthographicSize;
        m_Renderer = GetComponent<MeshRenderer>();
        //print("CAMERA!" + mirrorCamera.orthographicSize);
    }

    private void Update()
    {
        if (m_Renderer.isVisible)
        {
            mirrorCamera.gameObject.SetActive(true);
        }
        else
        {
            mirrorCamera.gameObject.SetActive(false);
        }
    }

    // Check if the object is reflected in the mirror
    public bool IsReflectedInMirror(Collider2D objCollider)
    {
        if (objCollider.enabled == false)
        {
            return false;
        }
        Vector2[] points = GetReflectionPoints(objCollider);

        foreach (Vector2 point in points)
        {
            Vector3 viewportPoint = mirrorCamera.WorldToViewportPoint(point);

            if (viewportPoint.z >= 0 &&
                viewportPoint.x >= 0f && viewportPoint.x <= 1f &&
                viewportPoint.y >= 0f && viewportPoint.y <= 1f)
            {
                return true; // Au moins un point est dans la vue de la caméra miroir
            }
        }

        return false;
        //planes = GeometryUtility.CalculateFrustumPlanes(mirrorCamera);
        //return GeometryUtility.TestPlanesAABB(planes, objCollider.bounds);
    }

    public Vector2[] GetReflectionPoints(Collider2D objCollider)
    {
        // Sample points from the object collider
        Vector2[] objectPoints = LightUtility.GetSamplePointsFromObject(objCollider);

        List<Vector2> reflectedPoints = new List<Vector2>();

        foreach (Vector2 point in objectPoints)
        {
            Vector3 reflectionPoint = mirrorCamera.WorldToViewportPoint(point);
            
            // If point is in camera's view it means the point is reflected
            if (reflectionPoint.x >= 0 && reflectionPoint.x <= 1 &&
               reflectionPoint.y >= 0 && reflectionPoint.y <= 1 &&
               reflectionPoint.z >= 0)
            {
                // Convert the viewport point back to world space
                Vector3 worldPoint = mirrorCamera.ViewportToWorldPoint(
                    new Vector3(reflectionPoint.x, reflectionPoint.y, mirrorCamera.nearClipPlane));
                reflectedPoints.Add(worldPoint);
                //reflectedPoints.Add(reflectionPoint);
            }
        }
        //print(reflectedPoints.Count);
        return reflectedPoints.ToArray();
    }

    // Check if there's anything blocking the mirror's view of the player
    public bool IsMirrorReflectionBlocked(Vector2[] reflectedPoints, Collider2D playerCollider)
    {
        //// Check if there is no object blocking the sight of the NPC
        if (playerCollider == null) return true;

        // Get the bounds of the player collider
        Bounds playerBounds = playerCollider.bounds;

        // Get the bounds of the mirror (assuming the mirror has a collider)
        //Bounds mirrorBounds = GetComponent<Collider2D>().bounds;
        Collider2D mirrorCollider  = GetComponent<Collider2D>();
        // Cast rays from multiple points on the mirror to various points on the player's collider
        Vector2[] mirrorPoints = LightUtility.GetSamplePointsFromObject(mirrorCollider);
        //Vector2[] mirrorPoints = {
        //new Vector2(mirrorBounds.center.x, mirrorBounds.center.y),                  // Center
        //new Vector2(mirrorBounds.min.x, mirrorBounds.min.y),                        // Bottom-left
        //new Vector2(mirrorBounds.max.x, mirrorBounds.min.y),                        // Bottom-right
        //new Vector2(mirrorBounds.min.x, mirrorBounds.max.y),                        // Top-left
        //new Vector2(mirrorBounds.max.x, mirrorBounds.max.y)};                      // Top-right


        //Vector2[] playerPoints = {
        //new Vector2(playerBounds.center.x, playerBounds.center.y),                  // Center
        //new Vector2(playerBounds.min.x, playerBounds.min.y),                        // Bottom-left
        //new Vector2(playerBounds.max.x, playerBounds.min.y),                        // Bottom-right
        //new Vector2(playerBounds.min.x, playerBounds.max.y),                        // Top-left
        //new Vector2(playerBounds.max.x, playerBounds.max.y)};                      // Top-right



        // Check if any ray from the mirror to the player is unobstructed
        foreach (Vector2 mirrorPoint in mirrorPoints)
        {
            foreach (Vector2 playerPoint in reflectedPoints)
            {
                Vector2 direction = (playerPoint - mirrorPoint).normalized;
                float distance = Vector2.Distance(mirrorPoint, playerPoint);

                // Cast ray from mirror point to player point
                RaycastHit2D hit = Physics2D.Raycast(mirrorPoint, direction, distance, ~ignoreLayer);

                // If ray directly hits the player without obstruction, reflection is not blocked
                if (hit.collider == playerCollider)
                {
                    return false; // Found at least one unobstructed view
                }
                else
                {
                    //print(hit.collider.gameObject.name);
                }
            }
        }

        // All rays were obstructed, so reflection is blocked
        return true;




        //Vector2 directionToObject = (playerCollider.transform.position - transform.position).normalized;
        //RaycastHit2D raycastHit = Physics2D.Raycast(transform.position, directionToObject, reflectionDistance, ~ignoreLayer);
        //if(raycastHit.collider != null && raycastHit.collider != playerCollider)
        //{
        //    return true;
        //}
        //else
        //{
        //    return false;
        //}
    }

    //private void OnBecameVisible()
    //{
    //    if (mirrorCamera != null)
    //    {
    //        mirrorCamera.gameObject.SetActive(true);
    //    }
    //}

    //private void OnBecameInvisible()
    //{
    //    if (mirrorCamera != null)
    //    {
    //        mirrorCamera.gameObject.SetActive(false);
    //    }
    //}
}
