using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using Unity.VisualScripting;
using UnityEngine;
using System.Drawing;

// Interface for NPC who are patrolling
public interface IPatrol
{
    IEnumerator Patrol();
    void MoveToNextAvailablePatrolPoint();
}
public enum PatrollingNPCState
{
    Idle,                   // NPC is available to patrol
    Patrolling,             // NPC is walking to the next patrol point
    WaitingAtPoint,         // NPC has reached a patrol point and is waiting
    InRoom,                 // NPC is inside a room
    Blocked,                // NPC is blocked by a possessed object at a room entrance
    Investigating,          // NPC is investigating a sound or event
    Returning               // NPC is returning to its initial position after investigation
}

[RequireComponent(typeof(PatrollingNPCInvestigationController))]
public class PatrollingNPCBehaviour : HumanNPCBehaviour, IPatrol, IResetInitialState
{
    // Extends HumanNPCBehaviour for patrolling NPCs.
    // Handles:
    // - Patrolling between patrol points
    // - Room entry and exit with door animations
    // - Blocked state when a possessed object blocks a room entrance
    // - Getting unstuck when the blocking object is removed


    [Header("Patrolling NPC Sound Variables")]
    [SerializeField] protected AK.Wwise.Event npcLockedSoundEvent;  // Played when the NPC is blocked at a room entrance
    [SerializeField] protected AK.Wwise.Event doorOpenSoundEvent;   // Played when the NPC opens a door
    [SerializeField] protected AK.Wwise.Event doorCloseSoundEvent;  // Played when the NPC closes a door

    [Header("Patrol variables")]
    [SerializeField] protected PatrolPointData[] patrolPoints;      // list of patrol destinations
    protected int indexPatrolPoints;                                // Index of the next patrol point to visit
    private Coroutine patrolCoroutine;                              // Currently running patrol coroutine
    PatrolPointData currentPoint;                                   // Patrol point the NPC is currently at
    PatrolPointData nextPatrolPoint;                                // Next patrol point the NPC will move to
    private PatrolPointData initialPatrolPoint;                     // First patrol point

    protected Animator animator;                                    // Controls room entry/exit animations
    
    // Getters
    public bool IsBlocked => CurrentState == PatrollingNPCState.Blocked;
    public bool IsInRoom => CurrentState == PatrollingNPCState.InRoom;
    public bool IsUnavailable => CurrentState == PatrollingNPCState.Blocked
                              || CurrentState == PatrollingNPCState.InRoom;

    // Getter and Setter
    public PatrollingNPCState CurrentState { get; set; } = PatrollingNPCState.Idle;


    protected override void Start()
    {
        base.Start();
        indexPatrolPoints = 0;
        animator = GetComponentInChildren<Animator>();
        currentPoint = null;

        // Can't play non suspicious sound if the NPC is blocked
        soundController.AddNonSuspiciousSoundConditions(() => !IsBlocked);

        if (patrolPoints.Length > 0)
        {
            nextPatrolPoint = patrolPoints[indexPatrolPoints];
            initialPatrolPoint = nextPatrolPoint;
        }
    }
    protected override void Update()
    {
        base.Update();

        // Priority 1: if blocked but the room is no longer blocked by a possessed object, get unstuck
        if (CurrentState == PatrollingNPCState.Blocked
            && currentPoint != null
            && !IsRoomBlocked(currentPoint))
        {
            npcLockedSoundEvent.Stop(gameObject);
            StartCoroutine(GetUnstuck());
            return;
        }

        // Priority 2: start patrolling if idle and no investigation is pending
        if (CurrentState == PatrollingNPCState.Idle
            && investigationController.QueueCount == 0
            && !investigationController.IsInvestigating)
        {
            patrolCoroutine = StartCoroutine(Patrol());
        }
    }

    // Moves the NPC to the next patrol point, then handles waiting behavior at that point.
    public IEnumerator Patrol()
    {
        if (patrolPoints.Length == 0 || nextPatrolPoint == null) yield break;

        CurrentState = PatrollingNPCState.Patrolling;
        currentPoint = null;

        // Move to the patrol point
        Vector2 destination = new Vector2(nextPatrolPoint.Point.position.x, transform.position.y);
        yield return npcMovementController.ReachTarget(destination, currentFloorLevel, nextPatrolPoint.FloorLevel);

        // Wait at the point
        currentPoint = nextPatrolPoint;
        yield return HandleWaiting(currentPoint);
    }

    // Stops the current patrol coroutine and halts NPC movement.
    // Called before starting an investigation.
    public void StopPatrolling()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }
        npcMovementController.StopMovement();
    }

    // Handles NPC behavior when arriving at a patrol point.
    // - Room points: enter, wait, exit OR enter blocked state if entrance is blocked
    // - Regular points: wait then continue
    protected IEnumerator HandleWaiting(PatrolPointData point)
    {
        CurrentState = PatrollingNPCState.WaitingAtPoint;

        if (point.PatrolPointType == PatrolPointType.Room)
        {
            // The Room is not blocked so the NPC Enter the room
            if (!IsInRoom && !IsRoomBlocked(point))
            {
                // The NPC can no longer detect anything while in the Room
                canSee = false;
                fovLight.enabled = false;

                doorOpenSoundEvent.Post(gameObject);
                animator.SetBool("EnterRoom", true);

                CurrentState = PatrollingNPCState.InRoom;
                yield return new WaitForSeconds(0.5f);
                doorCloseSoundEvent.Post(gameObject);
                yield return new WaitForSeconds(point.WaitTime);

                // If the Room is not blocked after the wait time
                if (!IsRoomBlocked(point))
                {
                    // Exit the room normally
                    doorOpenSoundEvent.Post(gameObject);
                    animator.SetTrigger("ExitRoom");
                    yield return new WaitForSeconds(0.5f);
                    doorCloseSoundEvent.Post(gameObject);

                    // The NPC can detect again
                    canSee = true;
                    fovLight.enabled = true;
                    animator.SetBool("EnterRoom", false);
                    CurrentState = PatrollingNPCState.Idle;
                }
                else
                {
                    // The NPC got blocked while inside the Room. Play locked sound and wait
                    npcLockedSoundEvent.Post(gameObject, (uint)AkCallbackType.AK_Marker, MarkerCallback);
                    CurrentState = PatrollingNPCState.Blocked;
                    yield break;
                }
            }
            // Entrance is blocked before entering. Wait briefly then continue
            else if (!IsInRoom && IsRoomBlocked(point))
            {
                yield return new WaitForSeconds(point.WaitTimeBlocked);
                CurrentState = PatrollingNPCState.Idle;
            }
            // The NPC got blocked while inside the Room. Play locked sound and wait
            else if (IsInRoom && IsRoomBlocked(point))
            {
                npcLockedSoundEvent.Post(gameObject, (uint)AkCallbackType.AK_Marker, MarkerCallback);
                CurrentState = PatrollingNPCState.Blocked;
                yield break;
            }
        }
        else
        {
            // Regular patrol point. Wait then continue
            yield return new WaitForSeconds(point.WaitTime);
            CurrentState = PatrollingNPCState.Idle;
        }

        MoveToNextAvailablePatrolPoint();
    }

    // Returns true if a possessed object is blocking enough of the room entrance
    // to prevent the NPC from entering.
    // Blocking is calculated as the percentage of entrance width covered by objects.
    protected bool IsRoomBlocked(PatrolPointData point)
    {
        // Only patrolPoint type of Room can be blocked
        if (point.PatrolPointType != PatrolPointType.Room || point.SpriteRenderer == null) return false;

        // Get the room sprite bounds, width and height
        Bounds roomBounds = point.SpriteRenderer.bounds;
        float roomWidth = roomBounds.size.x;
        float roomHeight = roomBounds.size.y;

        // Get object colliders in front of the Room
        Collider2D[] colliders = Physics2D.OverlapBoxAll(point.SpriteRenderer.transform.position,
                                                        new Vector2(roomWidth,roomHeight),
                                                        0f, detectObjectLayer
                                                        );

        float blockedWidth = 0f;
        foreach (Collider2D collider in colliders)
        {
            // Skip objects that are not tall enough to block the entrance
            if (collider.bounds.size.y < point.MinimumBlockHeight)          
                continue;

            // Calculate the overlapping width between the object and the room entrance
            float objectWidth = Mathf.Min(collider.bounds.max.x, roomBounds.max.x)
                                - Mathf.Max(collider.bounds.min.x, roomBounds.min.x);
            if(objectWidth > 0)
            {
                blockedWidth += objectWidth;
            }
        }

        // Calculate what percentage of the entrance is blocked
        float blockPercentage = blockedWidth / roomWidth;

        return blockPercentage >= point.BlockingThreshold;
    }

    // Wwise marker callback triggered during the locked sound event.
    // Fires the NPCBlocked animation trigger on the room animator.
    private void MarkerCallback(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info)
    {
        if (in_type == AkCallbackType.AK_Marker)
        {
            AkMarkerCallbackInfo markerInfo = (AkMarkerCallbackInfo)in_info;

            Animator roomAnimator = currentPoint.GetComponent<Animator>();
            if (roomAnimator != null)
            {
                roomAnimator.SetTrigger("NPCBlocked");

            }
        }
    }

    // Called when the blocking object has been removed.
    // Plays the exit room animation and restores normal patrol state.
    protected IEnumerator GetUnstuck()
    {
        // Animation of NPC coming out of the room
        CurrentState = PatrollingNPCState.WaitingAtPoint;

        animator.SetTrigger("ExitRoom");
        yield return new WaitForSeconds(0.5f);
        canSee = true;
        fovLight.enabled = true;
        animator.SetBool("EnterRoom", false);
        CurrentState = PatrollingNPCState.Idle;

        soundController.TryPlayNonSuspiciousSound();
        // After getting unstuck, move to the next patrol point
        MoveToNextAvailablePatrolPoint();
    }

    // Advances to the next patrol point in the list
    public void MoveToNextAvailablePatrolPoint()
    {
        int patrolPointPossibilities = patrolPoints.Length;

        indexPatrolPoints++;
        if (indexPatrolPoints >= patrolPoints.Length)
        {
            indexPatrolPoints = 0;
        }
        nextPatrolPoint = patrolPoints[indexPatrolPoints];
    }   

    // Hides the icon when the NPC is unavailable (inside a room or blocked)
    protected override IconState GetIconState()
    {
        if (IsUnavailable) return IconState.None;
        return base.GetIconState();
    }

    public override void ResetInitialState()
    {
        base.ResetInitialState();
        animator.Rebind();      
        fovLight.enabled = true;        
        canSee = true;
        currentPoint = null;
        patrolCoroutine = null;
        indexPatrolPoints = 0;
        CurrentState = PatrollingNPCState.Idle;

        if (initialPatrolPoint != null)
        {
            nextPatrolPoint = initialPatrolPoint;
        }
        npcLockedSoundEvent.Stop(gameObject);
    }
}
