using System.Collections.Generic;
using UnityEngine;

// Interface for objects emitting sound that must be reset
// after their sound event.
public interface IResetObject
{
    public void ResetObject();
}
public abstract class SoundDetection : MonoBehaviour
{
    // Base class for objects that emit sounds detectable by nearby NPCs.
    // When a sound event occurs, notifies all HumanNPCBehaviour instances within range.
    // Only the first NPC in the list is asked to replace the object after investigating.

    [SerializeField] protected float soundRadius;       // Radius within which NPCs can hear this object
    [SerializeField] protected LayerMask npcLayer;      // Layer of the NPCs who must be alerted by the sound
    [SerializeField] protected float floorLevel;        // Floor level of this object

    protected SoundEmittingObject objectType;           // Type of sound-emitting object

    // Getters
    public SoundEmittingObject ObjectType => objectType;
    public float FloorLevel => floorLevel;


    // Notifies all nearby NPCs of this sound event.
    // The first NPC notified can be instructed to replace the object
    // or stop the sound while the others just go investigate.
    // If no available NPCs exist, blocked NPCs are notified instead.
    protected void NotifyNearbyEnemies()
    {
        // Find all NPC within sound range
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
                    blockedNPCs.Add(npcPatrol);
                }
                else
                {
                    // The NPC is availabe to go and investigate
                    availableNPCs.Add(npc);
                }
            }
        }

        // Prefer available NPCs to replace or stop the sound, but fall back to blocked ones if they are all blocked.
        // Otherwise we ignore the blocked
        List<HumanNPCBehaviour> candidateNPCs = availableNPCs.Count > 0 ? availableNPCs : blockedNPCs;
        if (candidateNPCs.Count > 0)
            NotifyNPCs(candidateNPCs);
    }

    // Sends investigation requests to each NPC in the list.
    // Only the first NPC is asked to replace the object or stop the sound after investigating.
    // The reste only happens if the object is an IResetObject.
    private void NotifyNPCs(List<HumanNPCBehaviour> npcsList)
    {
        bool isFirst = true;
        foreach (HumanNPCBehaviour npc in npcsList)
        {            
            npc.InvestigateSound(this, isFirst, floorLevel);
            isFirst = false;
        }
    }

    protected void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, soundRadius);
    }

}

// Every type of sound emitting object in the game
public enum SoundEmittingObject
{
    FallingObject,
    SoundObject,
    BreakableObject
}
