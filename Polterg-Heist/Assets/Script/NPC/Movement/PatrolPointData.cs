using UnityEngine;

// Different type of patrolling point
public enum PatrolPointType
{
    Normal, // NPC waits briefly then continues patrolling
    Room    // NPC enters the room, waits inside, exits then continues patrolling
}
public class PatrolPointData : MonoBehaviour
{
    // Holds all configuration data for a single NPC patrol destination.    


    
    [SerializeField] private PatrolPointType patrolPointType;   // Type of the patrol point (Room or Normal)
    [SerializeField] private float floorLevel;                  // Floor level of this patrol point
    [SerializeField] private float waitTime;                    // Time the NPC waits at this point before moving on
    [SerializeField] private SpriteRenderer spriteRenderer;     // Sprite renderer of the room
    [SerializeField] private float minimumBlockHeight;          // Minimum height an object must have to count as blocking the entrance
    [SerializeField] private float blockingThreshold;           // Fraction of entrance width that must be blocked to prevent entry (e.g. 0.5 = 50%)
    [SerializeField] private float waitTimeBlocked;             // Time the NPC waits in front of a blocked entrance before moving on

    // Getters
    public Transform Point => transform;
    public PatrolPointType PatrolPointType => patrolPointType;
    public float WaitTime => waitTime;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public float MinimumBlockHeight => minimumBlockHeight;
    public float BlockingThreshold => blockingThreshold;
    public float WaitTimeBlocked => waitTimeBlocked;
    public float FloorLevel => floorLevel;

}
