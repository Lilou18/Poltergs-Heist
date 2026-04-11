using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using Unity.VisualScripting;
using UnityEngine;
using System.Drawing;

public interface IPatrol
{
    IEnumerator Patrol();
    void MoveToNextAvailablePatrolPoint();
}
public enum PatrollingNPCState
{
    Idle,
    Patrolling,
    WaitingAtPoint,
    InRoom,
    Blocked,
    Investigating,
    Returning
}
public class PatrollingNPCBehaviour : HumanNPCBehaviour, IPatrol, IResetInitialState
{
    [Header("Patrolling NPC Sound Variables")]
    [SerializeField] protected AK.Wwise.Event npcLockedSoundEvent;
    [SerializeField] protected AK.Wwise.Event doorOpenSoundEvent;
    [SerializeField] protected AK.Wwise.Event doorCloseSoundEvent;

    [Header("Patrol variables")]
    [SerializeField] protected PatrolPointData[] patrolPoints;    // Points were the NPC patrol
    protected int indexPatrolPoints;    // Next index patrol point    
    protected Animator animator;
    PatrolPointData currentPoint;   // Point where the NPC is located
    PatrolPointData nextPatrolPoint; // Next NPC patrol point
    private PatrolPointData initialPatrolPoint;

    public PatrollingNPCState CurrentState { get;  set; } = PatrollingNPCState.Idle;

    // Public properties to access from other scripts
    public bool IsBlocked => CurrentState == PatrollingNPCState.Blocked;
    public bool IsInRoom => CurrentState == PatrollingNPCState.InRoom;
    public bool IsUnavailable => CurrentState == PatrollingNPCState.Blocked
                              || CurrentState == PatrollingNPCState.InRoom;

    private Coroutine patrolCoroutine;
    protected override void Start()
    {
        base.Start();
        indexPatrolPoints = 0;
        animator = GetComponentInChildren<Animator>();
        currentPoint = null;

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

        // Priority 1: si bloqué mais la room est libre se débloquer
        if (CurrentState == PatrollingNPCState.Blocked
            && currentPoint != null
            && !IsRoomBlocked(currentPoint))
        {
            npcLockedSoundEvent.Stop(gameObject);
            StartCoroutine(GetUnstuck());
            return;
        }

        // Priority 2: patrouiller si disponible
        if (CurrentState == PatrollingNPCState.Idle
            && investigationController.QueueCount == 0
            && !investigationController.IsInvestigating)
        {
            patrolCoroutine = StartCoroutine(Patrol());
        }
    }

    protected override IconState GetIconState()
    {
        if (IsUnavailable) return IconState.None;
        return base.GetIconState();
    }

    public IEnumerator ReturnRightFloor()
    {
        yield return StartCoroutine(npcMovementController.ReachFloor(currentFloorLevel, initialFloorLevel));
        if (FloorLevel == initialFloorLevel)
        {
            //rightFloor = true;
        }

    }

    public IEnumerator Patrol()
    {
        if (patrolPoints.Length == 0 || nextPatrolPoint == null) yield break;

        CurrentState = PatrollingNPCState.Patrolling;
        currentPoint = null;

        Vector2 destination = new Vector2(nextPatrolPoint.Point.position.x, transform.position.y);
        yield return npcMovementController.ReachTarget(destination, currentFloorLevel, nextPatrolPoint.FloorLevel);

        currentPoint = nextPatrolPoint;
        yield return HandleWaiting(currentPoint);
    }

    public void StopPatrolling()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }
        npcMovementController.StopMovement(); // au lieu de Reset()
    }

    // Check if there is a possessed object in front of a room
    protected bool IsRoomBlocked(PatrolPointData point)
    {
        if (point.PatrolPointType != PatrolPointType.Room || point.SpriteRenderer == null) return false;


        // Get the room sprite bounds
        Bounds roomBounds = point.SpriteRenderer.bounds;

        // Get the sprite width and height
        float roomWidth = roomBounds.size.x;
        float roomHeight = roomBounds.size.y;

        // Get object colliders in front of the room
        Collider2D[] colliders = Physics2D.OverlapBoxAll(point.SpriteRenderer.transform.position,
                                                        new Vector2(roomWidth,roomHeight),
                                                        0f, detectObjectLayer
                                                        );

        float blockedWidth = 0f;

        foreach (Collider2D collider in colliders)
        {
            // Skip if the object is not tall enough
            if (collider.bounds.size.y < point.MinimumBlockHeight)          
                continue;
            
                

            // Calculate how much width of the room is the object taking
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

    protected IEnumerator HandleWaiting(PatrolPointData point)
    {
        CurrentState = PatrollingNPCState.WaitingAtPoint;

        if (point.PatrolPointType == PatrolPointType.Room)
        {
            if (!IsInRoom && !IsRoomBlocked(point))
            {
                // Entrer dans la room
                canSee = false;
                fovLight.enabled = false;
                doorOpenSoundEvent.Post(gameObject);
                animator.SetBool("EnterRoom", true);
                CurrentState = PatrollingNPCState.InRoom;
                yield return new WaitForSeconds(0.5f);
                doorCloseSoundEvent.Post(gameObject);
                yield return new WaitForSeconds(point.WaitTime);

                if (!IsRoomBlocked(point))
                {
                    doorOpenSoundEvent.Post(gameObject);
                    animator.SetTrigger("ExitRoom");
                    yield return new WaitForSeconds(0.5f);
                    doorCloseSoundEvent.Post(gameObject);
                    canSee = true;
                    fovLight.enabled = true;
                    animator.SetBool("EnterRoom", false);
                    CurrentState = PatrollingNPCState.Idle;
                }
                else
                {
                    //StopNonSuspiciousSound();
                    npcLockedSoundEvent.Post(gameObject, (uint)AkCallbackType.AK_Marker, MarkerCallback);
                    CurrentState = PatrollingNPCState.Blocked;
                    yield break;
                }
            }
            else if (!IsInRoom && IsRoomBlocked(point))
            {
                yield return new WaitForSeconds(point.WaitTimeBlocked);
                CurrentState = PatrollingNPCState.Idle;
            }
            else if (IsInRoom && IsRoomBlocked(point))
            {
                //StopNonSuspiciousSound();
                npcLockedSoundEvent.Post(gameObject, (uint)AkCallbackType.AK_Marker, MarkerCallback);
                CurrentState = PatrollingNPCState.Blocked;
                yield break;
            }
        }
        else
        {
            yield return new WaitForSeconds(point.WaitTime);
            CurrentState = PatrollingNPCState.Idle;
        }

        MoveToNextAvailablePatrolPoint();
    }

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


    // NPC is not blocked anymore
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
