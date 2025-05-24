using System.Collections.Generic;
using UnityEngine;

public class FloorNavigation : MonoBehaviour
{
    // Pathfinding for our Human NPC

    [SerializeField] private LayerMask stairLayer;
    // Dictionaries of all staircases ordered by floor
    private Dictionary<float, List<StairController>> stairsByFloorLevel = new Dictionary<float, List<StairController>>();

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
        //listAllStairs();
    }
    private void Start()
    {
        listAllStairs();
    }

    
    // List all stairs in the scene and ordered them by floor level
    private void listAllStairs()
    {
        StairController[] allStairs = FindObjectsByType<StairController>(FindObjectsSortMode.None); // Find all the stairs in the level
        
        foreach (StairController stair in allStairs)
        {
            float floorLevel = stair.FloorLevel;

            if (!stairsByFloorLevel.ContainsKey(floorLevel))
            {
                stairsByFloorLevel[floorLevel] = new List<StairController>();
            }
            
            stairsByFloorLevel[floorLevel].Add(stair);
            
        }
    }

    // Find the closest staircase and on the same floor as the npc.
    public StairController FindNearestStairToFloor(FloorNavigationRequest floorRequest, StairDirection neededDirection, List<StairController> excludeStairs)
    {
        float currentFloor = floorRequest.CurrentFloorLevel;
        float targetFloor = floorRequest.TargetFloorLevel;
        //print("CURRENT FLOOR " + currentFloor);
        //print("TARGET FLOOR " + targetFloor);
        // If we're already on the right floor, no stairs needed
        if (currentFloor == targetFloor)
        {
            return null;
        }

        if (!stairsByFloorLevel.ContainsKey(currentFloor))
        {
            print("GROSSE ERREUR");
            // No stairs found on current floor level
            return null;
        }

        StairController closestStair = null;
        float closestDistance = float.MaxValue;        

        // Find an available stair that leads to the targeted floor level
        foreach (StairController stair in stairsByFloorLevel[currentFloor])
        {
            if(currentFloor == 5)
            {
                print("TEST STAIR");
            }
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
        if(closestStair != null)
        {
            //print(closestStair.gameObject.name);
        }
        return closestStair;
    }

    // Check if the NPC can use this stair
    private bool CanNPCUseStair(StairController stair, FloorNavigationRequest floorRequest)
    {
        Renderer npcRenderer = floorRequest.ObjectRenderer;

        return npcRenderer.bounds.size.x <= stair.MaximumWidth && npcRenderer.bounds.size.y <= stair.MaximumHeight;
    }

    // Find all the stairs the NPC must used to go to the targeted floor
    public List<StairController> FindPathToFloor(FloorNavigationRequest floorRequest)
    {
        int safetyCounter = 100;
        List<StairController> path = new List<StairController>();   // List of all the stairs the NPC must used
        float currentFloor = floorRequest.CurrentFloorLevel;    // Floor level where the NPC is
        float targetFloor = floorRequest.TargetFloorLevel;
        
        // As long as the NPC is not on the desired floor
        while(currentFloor != targetFloor && safetyCounter > 0)
        {
            safetyCounter--;
            if (safetyCounter == 0)
            {
                Debug.LogError("Boucle infinie détectée dans FindPathToFloor !");
                return null;
            }
            // Determine if we need to go up or down
            StairDirection direction = (targetFloor > currentFloor) ? StairDirection.Upward : StairDirection.Downward;
            //print("CHECKING");
            //print(direction);
            //print(targetFloor);
            //print(currentFloor);
            // Find the nearest stair to used to go to the targeted floor
            StairController nextStair = FindNearestStairToFloor(floorRequest, direction, null);

            // If there is no path for the NPC
            if(nextStair == null)
            {
                print("cant find path");
                return null;
                // Can't find a path
                //break;
            }
            // Add the found staircase to the list the NPC must used
            path.Add(nextStair);

            //print(nextStair.gameObject.name);
            // Update floor level of the NPC
            if(direction == StairDirection.Upward && nextStair.UpperFloor != null)
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
    public Vector2 Position;
    public float CurrentFloorLevel;
    public float TargetFloorLevel;
    public Renderer ObjectRenderer;

    public FloorNavigationRequest(Vector2 position, float currentFloorLevel, float targetFloorLevel, Renderer renderer)
    {
        Position = position;
        CurrentFloorLevel = currentFloorLevel;
        TargetFloorLevel = targetFloorLevel;
        ObjectRenderer = renderer;
    }
}
