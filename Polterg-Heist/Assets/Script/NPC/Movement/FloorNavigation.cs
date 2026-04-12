using System.Collections.Generic;
using UnityEngine;

public class FloorNavigation : MonoBehaviour
{
    // Handles floor to floor navigation for NPCs using a stair based system.
    // In this system, stairs are treated as navigation nodes connecting floors.
    // Each StairController links to another stair on a different floor (UpperFloor / BottomFloor).

    // Dictionary storing all stairs grouped by floor level
    private Dictionary<float, List<StairController>> stairsByFloorLevel = new Dictionary<float, List<StairController>>();

    // Getter
    public Dictionary<float, List<StairController>> StairsByFloorLevel => stairsByFloorLevel;

    // Singleton pattern
    private static FloorNavigation instance;
    public static FloorNavigation Instance { get { return instance; } }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    private void Start()
    {
        ListAllStairs();
    }


    // Finds all StairController instances in the scene
    // and groups them by floor level for fast lookup during navigation.
    private void ListAllStairs()
    {
        // Find all the stairs in the level
        StairController[] allStairs = FindObjectsByType<StairController>(FindObjectsSortMode.None);
        
        foreach (StairController stair in allStairs)
        {
            float floorLevel = stair.FloorLevel;

            // Create list for this floor if it doesn't exist
            if (!stairsByFloorLevel.ContainsKey(floorLevel))
            {
                stairsByFloorLevel[floorLevel] = new List<StairController>();
            }
            
            stairsByFloorLevel[floorLevel].Add(stair);            
        }
    }

    // Finds the closest usable stair on the current floor that moves the NPC toward the target floor.
    // 1. Filter stairs on the current floor
    // 2. Exclude already tried stairs (to avoid loops)
    // 3. Keep only stairs that go in the required direction (up/down)
    // 4. Check size constraints (NPC must fit)
    // 5. Return the closest valid stair
    public StairController FindNearestStairToFloor(FloorNavigationRequest floorRequest, StairDirection neededDirection, List<StairController> excludeStairs)
    {
        float currentFloor = floorRequest.CurrentFloorLevel;
        float targetFloor = floorRequest.TargetFloorLevel;

        // If we're already on the right floor, no stairs needed
        if (currentFloor == targetFloor)
        {
            return null;
        }

        if (!stairsByFloorLevel.ContainsKey(currentFloor))
        {
            // No stairs found on current floor level
            return null;
        }

        StairController closestStair = null;
        float closestDistance = float.MaxValue;        

        // Find an available stair that leads to the targeted floor level
        foreach (StairController stair in stairsByFloorLevel[currentFloor])
        {
            // Skip this stair if it's in the exclude list
            if (excludeStairs != null && excludeStairs.Contains(stair))
            {
                continue;
            }
            // Check if this stair leads to the right direction
            bool canUseStair = false;

            // If the NPC needs to go up and the stair leads upward
            if(neededDirection == StairDirection.Upward && stair.UpperFloor != null && stair.UpperFloor.FloorLevel <= targetFloor)
            {
                canUseStair = true;
            }
            // If the NPC nees to go down and the stair leads downstair
            else if (neededDirection == StairDirection.Downward && stair.BottomFloor != null && stair.BottomFloor.FloorLevel >= targetFloor)
            {
                canUseStair = true;
            }
            // If a stair was found we checked if it's the closest one
            if (canUseStair && CanNPCUseStair(stair, floorRequest))
            {
                float distance = Vector2.Distance(floorRequest.Position, stair.StartPoint.transform.position);
                if(distance < closestDistance)
                {
                    closestDistance = distance;
                    closestStair = stair;
                }
            }
        }
        
        return closestStair;
    }

    // Ensures the NPC can physically use the stair based on its size.
    // Prevents NPC from using small stair passages.
    private bool CanNPCUseStair(StairController stair, FloorNavigationRequest floorRequest)
    {
        Renderer npcRenderer = floorRequest.ObjectRenderer;

        return npcRenderer.bounds.size.x <= stair.MaximumWidth && npcRenderer.bounds.size.y <= stair.MaximumHeight;
    }

    // Find all the stairs the NPC must used to reach the target floor
    // A safety counter prevents infinite loops in malformed setups in the level
    public List<StairController> FindPathToFloor(FloorNavigationRequest floorRequest)
    {
        int safetyCounter = 100;
        List<StairController> path = new List<StairController>();   // List of all the stairs the NPC must used
        float currentFloor = floorRequest.CurrentFloorLevel;        // Floor level where the NPC is
        float targetFloor = floorRequest.TargetFloorLevel;          // Floor level we want to reach
        
        // As long as the NPC is not on the desired floor
        while(currentFloor != targetFloor && safetyCounter > 0)
        {
            safetyCounter--;
            if (safetyCounter == 0)
            {
                Debug.LogError("Infinite loop detected in FindPathToFloor!");
                return null;
            }

            // Determine movement direction based on target floor
            StairDirection direction = (targetFloor > currentFloor) ? StairDirection.Upward : StairDirection.Downward;

            // Find the nearest stair to used to go to the targeted floor
            StairController nextStair = FindNearestStairToFloor(floorRequest, direction, null);

            // No path found for the NPC
            if(nextStair == null)
            {
                Debug.Log("No path found to target floor.");
                return null;
            }
            // Add the found staircase to the list the NPC must used
            path.Add(nextStair);

            // After finding a stair, update the current floor
            if (direction == StairDirection.Upward && nextStair.UpperFloor != null)
            {
                currentFloor = nextStair.UpperFloor.FloorLevel;   
                floorRequest.CurrentFloorLevel = currentFloor;
                
            }
            else if(direction == StairDirection.Downward && nextStair.BottomFloor != null)
            {
                currentFloor = nextStair.BottomFloor.FloorLevel;
                floorRequest.CurrentFloorLevel = currentFloor;
            }
        }
        return path;
    }

}

// Simple struct to pass navigation request data
public struct FloorNavigationRequest
{
    public Vector2 Position;                // NPC world position
    public float CurrentFloorLevel;         // NPC current floor
    public float TargetFloorLevel;          // Desired floor to reach
    public Renderer ObjectRenderer;         // Used for size constraint checks

    public FloorNavigationRequest(Vector2 position, float currentFloorLevel, float targetFloorLevel, Renderer renderer)
    {
        Position = position;
        CurrentFloorLevel = currentFloorLevel;
        TargetFloorLevel = targetFloorLevel;
        ObjectRenderer = renderer;
    }
}
