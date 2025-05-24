using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PowerOutage : MonoBehaviour, IPossessable, IResetInitialState
{
    [Header("Lights variables")]
    [SerializeField] private bool closeAllLightsInBuilding;  // Do we close all the lights inside the house
    [SerializeField] private GameObject[] closedLights; // Specific lights that should be close
    [SerializeField] private LayerMask wallFloorLayer;
    [SerializeField] private float floorLevel;
    [SerializeField] private float repairingTime;   // Time used by the npc to repair the lights
    private GameObject[] allBuildingLights;   // Every lights inside the building
    private GameObject[] lightsToClose;
    private Animator animator;

    private List<HumanNPCBehaviour> affectedNPCs = new List<HumanNPCBehaviour>();
    private bool isRepairing = false;
    
    void Start()
    {
        allBuildingLights = GameObject.FindGameObjectsWithTag("BuildingLight");
        animator = GetComponent<Animator>();
    }

    public void OnDepossessed()
    {
        // Nothing to implement when player depossessed the object
    }

    public void OnPossessed()
    {
        lightsToClose = closeAllLightsInBuilding ?  allBuildingLights : closedLights;
        CloseOpenLights(lightsToClose, false );
        animator.SetBool("isElectricityClosed", true);
    }

    // Close or open all lights in the array
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

    // Handle NPC behaviours when the lights are closed
    private void HandleNPCs(GameObject[] lightsClosed)
    {
        // Find all NPC in the scene
        HumanNPCBehaviour[] allNPCs = FindObjectsByType<HumanNPCBehaviour>(FindObjectsSortMode.None);

        List<HumanNPCBehaviour> availableNPCs = new List<HumanNPCBehaviour>();
        List<HumanNPCBehaviour> blockedNPCs = new List<HumanNPCBehaviour>();

        foreach (HumanNPCBehaviour npc in allNPCs)
        {
            //print("NPC");
            if (IsNPCAffected(npc, lightsClosed))
            {
                //print("YES HERE");
                PatrollingNPCBehaviour npcPatrol = npc.GetComponent<PatrollingNPCBehaviour>();
                // Check if the NPC is blocked
                if (npcPatrol != null && (npcPatrol.IsBlocked || npcPatrol.IsInRoom))
                {
                    blockedNPCs.Add(npcPatrol);
                }
                else
                {
                    // The NPC is availabe to go an investigate
                    availableNPCs.Add(npc);
                }
            }
            else
            {
                //print("NOT AFFECYED");
            }
        }
        if(availableNPCs.Count > 0)
        {
            NotifyNPCs(availableNPCs, lightsClosed);
        }
        else if(blockedNPCs.Count > 0)
        {
            NotifyNPCs(blockedNPCs, lightsClosed);
        }
    }

    private void NotifyNPCs(List<HumanNPCBehaviour> npcList, GameObject[] lightsClosed)
    {
        foreach (HumanNPCBehaviour npc in npcList)
        {
            // Surprise sound
            npc.audioSource.Play();
            if (!isRepairing)
            {
                isRepairing = true;
                npc.EnqueueInvestigation(RepairLights(npc, lightsClosed));
                print("Électricien : " + npc.gameObject.transform.parent.gameObject.name);
            }
            npc.NpcMovementController.ChangeSpeed();
            affectedNPCs.Add(npc);

        }
    }

    // Check if the NPC is currently in an area affected by the closed lights
    private bool IsNPCAffected(HumanNPCBehaviour npc, GameObject[] lights)
    {
        Collider2D npcCollider = npc.GetComponent<Collider2D>();
        foreach(GameObject gameObjectLight in lights)
        {
            
            Collider2D lightCollider = gameObjectLight.GetComponent<Collider2D>();
            if (LightUtility.IsPointHitByLight(lightCollider, npcCollider, wallFloorLayer))
            {
                print("SHOULD BE TRUE");
                return true;
            }
        }
        return false;
    }

    private IEnumerator RepairLights(HumanNPCBehaviour npc, GameObject[] lightsClosed)
    {
        // The NPC walk to the light switch
        yield return StartCoroutine(npc.NpcMovementController.ReachTarget(this.transform.position, npc.FloorLevel ,floorLevel));

        if (!npc.NpcMovementController.CanFindPath)
        {
            yield break;
        }

        // Time for the NPC to repair the problem 
        yield return new WaitForSeconds(repairingTime);
        animator.SetBool("isElectricityClosed", false);
        isRepairing = false;
        RestoreLights();
        
    }
    // Restore the closed lights
    private void RestoreLights()
    {
        // Open the lights back on
        CloseOpenLights(lightsToClose, true);
        lightsToClose = null;
        
        // NPCs have their normal behavior again
        foreach (HumanNPCBehaviour npc in affectedNPCs)
        {
            npc.NpcMovementController.ChangeSpeed();
            //PatrollingNPCBehaviour patrollingNPC = npc.gameObject.GetComponent<PatrollingNPCBehaviour>();
            //if (patrollingNPC != null)
            //{
            //    ///canMove = true;
            //    // movementspeed réduit
            //    /////TODO

            //}
        }
        affectedNPCs.Clear();
    }

    public void ResetInitialState()
    {
        StopAllCoroutines();
        lightsToClose = closeAllLightsInBuilding ? allBuildingLights : closedLights;
        CloseOpenLights(lightsToClose, true);
        lightsToClose = null;
        isRepairing = false;
        animator.SetBool("isElectricityClosed", false);
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
