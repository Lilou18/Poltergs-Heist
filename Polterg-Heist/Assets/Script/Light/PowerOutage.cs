using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PowerOutage : MonoBehaviour, IPossessable, IResetInitialState
{
    // Triggered when the player possesses this object.
    // Cuts the lights in a defined area, sends affected NPCs to investigate and repair,
    // then restores the lights once repairs are done.

    [Header("Lights")]
    [SerializeField] private bool closeAllLightsInBuilding; // If true, all building lights are turned off on possession
    [SerializeField] private GameObject[] closedLights;     // Specific lights to turn off if closeAllLightsInBuilding is false
    [SerializeField] private LayerMask wallFloorLayer;      // Floor and Wall layer
    [SerializeField] private float floorLevel;              // Floor level of the light switch, used for NPC pathfinding
    [SerializeField] private float repairingTime;           // Time the NPC spends repairing before restoring lights

    private GameObject[] allBuildingLights;                 // All GameObjects tagged "BuildingLight"
    private GameObject[] lightsToClose;                     // The active set of lights being managed
    private Animator animator;                              // Controls the light switch animation

    private List<HumanNPCBehaviour> affectedNPCs = new List<HumanNPCBehaviour>();   // NPCs whose speed was changed
    private bool isRepairing = false;                       // True while an NPC is walking to or performing the repair

    void Start()
    {
        allBuildingLights = GameObject.FindGameObjectsWithTag("BuildingLight");
        animator = GetComponent<Animator>();
    }

    public void OnDepossessed()
    {
        // Nothing to do on depossession — lights are restored by the NPC repair coroutine
    }

    // Triggers the power outage when the player possesses the switch.
    public void OnPossessed()
    {
        lightsToClose = closeAllLightsInBuilding ?  allBuildingLights : closedLights;
        CloseOpenLights(lightsToClose, false );
        animator.SetBool("isElectricityClosed", true);
    }

    // Activates or deactivates all lights in the array.
    // When closing lights, also notifies affected NPCs to investigate.
    public void CloseOpenLights(GameObject[] lights, bool open)
    {
        foreach (GameObject light in lights)
        {
            light.SetActive(open);
        }
        // If the lights are closed
        if (!open)
        {
            HandleNPCs(lights);
        }
    }

    // Finds NPCs illuminated by the affected lights.
    // Assigns one NPC to repair the lights and slows all affected NPCs.
    private void HandleNPCs(GameObject[] lightsClosed)
    {
        // Find all NPC in the scene
        HumanNPCBehaviour[] allNPCs = FindObjectsByType<HumanNPCBehaviour>(FindObjectsSortMode.None);

        List<HumanNPCBehaviour> availableNPCs = new List<HumanNPCBehaviour>();
        List<HumanNPCBehaviour> blockedNPCs = new List<HumanNPCBehaviour>();

        // Find the NPCs who no longer have lights
        foreach (HumanNPCBehaviour npc in allNPCs)
        {
            if (IsNPCAffected(npc, lightsClosed))
            {
                PatrollingNPCBehaviour npcPatrol = npc.GetComponent<PatrollingNPCBehaviour>();
                // Check if the NPC is blocked
                if (npcPatrol != null && (npcPatrol.IsBlocked || npcPatrol.IsInRoom))
                {
                    blockedNPCs.Add(npcPatrol);
                }
                else
                {
                    // The NPC is availabe to go and investigate
                    availableNPCs.Add(npc);
                }

            }
        }

        // Prefer available NPCs to repair the lights, but fall back to blocked ones if they are all blocked.
        // Otherwise we ignore the blocked
        List<HumanNPCBehaviour> candidateNPCs = availableNPCs.Count > 0 ? availableNPCs : blockedNPCs;
        if (candidateNPCs.Count > 0)
            NotifyNPCs(candidateNPCs, lightsClosed);
    }

    // Assigns the first available NPC to repair the lights.
    // Slows all affected NPCs to simulate panic/darkness behavior.
    private void NotifyNPCs(List<HumanNPCBehaviour> npcList, GameObject[] lightsClosed)
    {
        foreach (HumanNPCBehaviour npc in npcList)
        {
            if (!isRepairing)
            {
                isRepairing = true;
                // Start the investigation for the one who has to repair it
                npc.EnqueueInvestigation(RepairLights(npc, lightsClosed));
            }

            // Slow the NPC down to simulate navigating in the dark
            npc.NpcMovementController.ChangeSpeed();
            affectedNPCs.Add(npc);
        }
    }

    // Returns true if the NPC is currently illuminated by any of the affected lights.
    // Used to determine which NPCs should react to the power outage.
    private bool IsNPCAffected(HumanNPCBehaviour npc, GameObject[] lights)
    {
        Collider2D npcCollider = npc.GetComponent<Collider2D>();
        foreach(GameObject gameObjectLight in lights)
        {
            
            Collider2D lightCollider = gameObjectLight.GetComponent<Collider2D>();
            // Light is blocked by wall and floor
            if (LightUtility.IsPointHitByLight(lightCollider, npcCollider, wallFloorLayer))
            {                
                return true;
            }
        }
        return false;
    }

    // Moves the NPC to the light switch, waits for the repair, then restores lights.
    private IEnumerator RepairLights(HumanNPCBehaviour npc, GameObject[] lightsClosed)
    {
        // Step 1: Navigate to the light switch
        yield return StartCoroutine(npc.NpcMovementController.ReachTarget(this.transform.position, npc.FloorLevel ,floorLevel));

        // Abort if no valid path was found
        if (!npc.NpcMovementController.CanFindPath)
        {
            yield break;
        }

        // Step 2: Wait for the NPC to repair
        yield return new WaitForSeconds(repairingTime);

        // Step 3: Restore lights and reset state
        animator.SetBool("isElectricityClosed", false);
        isRepairing = false;
        RestoreLights();
        
    }
    // Restores all closed lights and resets NPC movement speed.
    private void RestoreLights()
    {
        // Open the lights back on
        CloseOpenLights(lightsToClose, true);
        lightsToClose = null;
        
        // NPCs have their normal behaviour again
        foreach (HumanNPCBehaviour npc in affectedNPCs)
        {
            npc.NpcMovementController.ChangeSpeed();
        }
        affectedNPCs.Clear();
    }

    public void ResetInitialState()
    {
        StopAllCoroutines();
        // Restore whichever lights were closed
        lightsToClose = closeAllLightsInBuilding ? allBuildingLights : closedLights;
        CloseOpenLights(lightsToClose, true);
        lightsToClose = null;

        isRepairing = false;
        animator.SetBool("isElectricityClosed", false);

        // Restore whichever lights were closed
        if (affectedNPCs.Count > 0)
        {
            foreach(HumanNPCBehaviour npc in affectedNPCs)
            {
                npc.NpcMovementController.ResetMovementSpeed();
            }
            affectedNPCs.Clear();
        }
    }
}
