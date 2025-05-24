using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public interface IResetObject
{
    public void ResetObject();
}
public abstract class SoundDetection : MonoBehaviour
{
    // Manage the detection of the sounds

    [SerializeField] protected float soundRadius;       // Radius in which the sound is projected
    [SerializeField] protected LayerMask npcLayer;      // Layer of the NPCs who must be alerted by the sound
    [SerializeField] protected float floorLevel;        // On which floor level is the object
    protected bool firstNPCNotified = false;            // Is there an NPC that was already notified of the sound
    protected SoundEmittingObject objectType;           // Which object type is the object

    protected AudioSource audioSource;

    // Getters
    public SoundEmittingObject ObjectType => objectType;
    public float FloorLevel => floorLevel;

    protected virtual void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Notify every enemies in the zone of the sound
    protected void NotifyNearbyEnemies(SoundDetection objectSound)
    {
        // Find all NPC within range
        Collider2D[] colliderNearbyNPC = Physics2D.OverlapCircleAll(transform.position, soundRadius, npcLayer);

        List<HumanNPCBehaviour> availableNPCs = new List<HumanNPCBehaviour>();
        List<HumanNPCBehaviour> blockedNPCs = new List<HumanNPCBehaviour>();

        foreach (Collider2D colliderNPC in colliderNearbyNPC)
        {
            HumanNPCBehaviour npc = colliderNPC.GetComponent<HumanNPCBehaviour>();
            if (npc != null)
            {
                PatrollingNPCBehaviour npcPatrol = npc.GetComponent<PatrollingNPCBehaviour>();
                // Check if the NPC is blocked
                if(npcPatrol != null && (npcPatrol.IsBlocked || npcPatrol.IsInRoom))
                {
                    //print("not available");
                    blockedNPCs.Add(npcPatrol);
                }
                else
                {
                    //print("Avaialble");
                    // The NPC is availabe to go an investigate
                    availableNPCs.Add(npc);
                }
            }
        }

        if(availableNPCs.Count > 0)
        {
            NotifyNPCs(availableNPCs, floorLevel, objectSound);
        }
        else if(blockedNPCs.Count > 0)
        {
            // Every NPCs are blocked
            NotifyNPCs(blockedNPCs, floorLevel, objectSound);
        }
    }

    private void NotifyNPCs(List<HumanNPCBehaviour> npcsList, float floorLevel, SoundDetection objectSound)
    {
        foreach(HumanNPCBehaviour npc in npcsList)
        {
            bool isFirst = !firstNPCNotified;
            npc.InvestigateSound(objectSound,isFirst,floorLevel);
            firstNPCNotified = true;
        }
        firstNPCNotified = false;
    }

    protected void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, soundRadius);
    }

}

public enum SoundEmittingObject
{
    FallingObject,
    SoundObject,
    BreakableObject
}
