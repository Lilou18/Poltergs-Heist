using UnityEngine;

// Different type of patrolling point
public enum PatrolPointType
{
    Normal,
    Room
}
public class PatrolPointData : MonoBehaviour
{
    // This class contains all the data for a patrol point of the NPC
    private Transform point;   // Destination point of the NPC
    [SerializeField] private PatrolPointType patrolPointType;   // Type of the destination
    [SerializeField] private float floorLevel;   // On what floor is this patrol point
    [SerializeField] private float waitTime;  // Waiting time before going to the next point
    [SerializeField] private SpriteRenderer spriteRenderer; // Sprite renderer of the room
    [SerializeField] private float minimumBlockHeight;  // Minimum Height of an object to block a room
    [SerializeField] private float blockingThreshold;   // How much of the door width needs to be blocked (0.5 = 50%)
    [SerializeField] private float waitTimeBlocked; // How much time we stay in front of a blocked room

    private void Start()
    {
        point = this.transform;
    }
    public Transform Point => point;
    public PatrolPointType PatrolPointType => patrolPointType;
    public float WaitTime => waitTime;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public float MinimumBlockHeight => minimumBlockHeight;
    public float BlockingThreshold => blockingThreshold;
    public float WaitTimeBlocked => waitTimeBlocked;
    public float FloorLevel => floorLevel;

}
