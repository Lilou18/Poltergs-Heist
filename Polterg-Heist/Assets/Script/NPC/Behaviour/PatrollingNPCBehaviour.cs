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
public class PatrollingNPCBehaviour : HumanNPCBehaviour, IPatrol, IResetInitialState
{
    [Header("Patrolling NPC Sound Variables")]
    [SerializeField] protected AK.Wwise.Event npcLockedSoundEvent;
    [SerializeField] protected AK.Wwise.Event doorOpenSoundEvent;
    [SerializeField] protected AK.Wwise.Event doorCloseSoundEvent;
    // Enemy patrol variables
    [Header("Patrol variables")]
    [SerializeField] protected PatrolPointData[] patrolPoints;    // Points were the NPC patrol
    protected int indexPatrolPoints;    // Next index patrol point    
    protected Animator animator;
    protected bool isBlocked;   // Is the NPC blocked in a room
    protected bool isWaiting;   // We wait for the animation to finish
    protected bool isInRoom;    // Is the NPC in a room
    PatrolPointData currentPoint;   // Point where the NPC is located
    PatrolPointData nextPatrolPoint; // Next NPC patrol point
    private PatrolPointData initialPatrolPoint;
    //private bool rightFloor;    // Does the NPC on the right floor to do is patrol
    //private bool isWalkingBack;    // Does the NPC go up the stairs
    private bool isPatrolling;


    // Public properties to access from other scripts
    public bool IsBlocked => isBlocked;
    public bool IsInRoom => isInRoom;

    private Coroutine returnToFloor;
    private Coroutine patrolling;
    protected override void Start()
    {
        base.Start();
        indexPatrolPoints = 0;
        animator = GetComponentInChildren<Animator>();
        //if(animator != null)
        //{
        //    print("Nom de l'animator" + animator.gameObject.name);
        //}
        isBlocked = false;
        isWaiting = false;
        isInRoom = false;
        //rightFloor = true;
        //isWalkingBack = false;
        isPatrolling = false;   
        currentPoint = null;
        if(patrolPoints.Length > 0)
        {
            nextPatrolPoint = patrolPoints[indexPatrolPoints];
            initialPatrolPoint = nextPatrolPoint;
        }
        
    }
    protected override void Update()
    {
        if(npcSpriteRenderer == null)
        {
            print("WTF");
        }

        UpdateIconDisplay();

        DetectMovingObjects();

        //if (hasSeenMovement && alertSpriteRenderer != null)
        //{
        //    if (SuspicionManager.Instance.HasSuspicionDecrease)
        //    {
        //        alertSpriteRenderer.enabled = false;
        //        print("already here");
        //    }

        //    if (SuspicionManager.Instance.CurrentSuspicion <= 0)
        //    {
        //        hasSeenMovement = false;
        //    }

        //}
        //if (hasSeenMovement && alertIcon != null)
        //{
        //    // Keep alert icon visible while suspicion exists, hide it when suspicion is gone
        //    if (SuspicionManager.Instance.CurrentSuspicion > 0)
        //    {
        //        alertIcon.enabled = true;
        //    }
        //    else
        //    {
        //        alertIcon.enabled = false;
        //        hasSeenMovement = false; // Reset the flag when suspicion is gone
        //    }
        //}

        CheckMirrorReflection();


        // Priority 1: If the NPC is blocked but the room is no longer blocked, get unstuck
        if(isBlocked && currentPoint != null && !IsRoomBlocked(currentPoint))
        {
            npcLockedSoundEvent.Stop(gameObject);
            StopNonSuspiciousSound();
            StartCoroutine(GetUnstuck());
            return;
        }
        // Priority 2: Handle investigation queue if we're not currently investigating or getting unstuck
        if(investigationQueue.Count > 0 && !isInvestigating && !isWaiting)
        {
            StopNonSuspiciousSound();
            if (returnToFloor != null)
            {
                StopCoroutine(returnToFloor);
                returnToFloor = null;
            }
            isInvestigating = true;
            //StopCoroutine("HandleWaiting");
            //StopCoroutine("Patrol");
            if(patrolling != null)
            {
                StopCoroutine(patrolling);
            }
            
            isPatrolling = false;
            IEnumerator investigationCoroutine = investigationQueue.Dequeue();
            StartCoroutine(RunInvestigation(investigationCoroutine));
        }
        // Priority 3: Return to starting floor if we need to
        //else if(investigationQueue.Count == 0 && !isInvestigating && !rightFloor && isWalkingBack)
        //{
        //    isWalkingBack = false;
        //    returnToFloor = StartCoroutine(ReturnRightFloor());
        //}
        // Priority 4: Patrol if we're able to and should be
        else if(investigationQueue.Count == 0 && !isInvestigating && !isWaiting && !isBlocked && !isPatrolling)
        {
            //nonSuspiciousSoundEvent.Post(gameObject);
            patrolling = StartCoroutine(Patrol());
        }


        // Handle ambient sound for patrolling NPCs
        //bool shouldPlayAmbientSound = CanPlayNonSuspiciousSound() && !isNonSuspiciousSoundPlaying && nonSuspiciousSoundCoroutine == null;
        bool shouldStopAmbientSound = (!CanPlayNonSuspiciousSound() || isInvestigating || investigationQueue.Count > 0) && isNonSuspiciousSoundPlaying;

        // Stop the sound if conditions require it
        if (shouldStopAmbientSound)
        {
            StopNonSuspiciousSound();
        }
        // Start the sound if conditions allow it and we're not already playing/about to play
        //else if (shouldPlayAmbientSound)
        //{
        //    StartNonSuspiciousSound();
        //}

    }

    protected override void UpdateIconDisplay()
    {
        if(isBlocked || isInRoom)
        {
            if(alertSpriteRenderer != null)
            {
                alertSpriteRenderer.enabled = false;
            }
            return;
        }

        base.UpdateIconDisplay();
    }

    // Override to add patrolling-specific conditions
    protected override bool CanPlayNonSuspiciousSound()
    {
        bool baseConditions =  base.CanPlayNonSuspiciousSound();

        return baseConditions && !isBlocked; // !isWaiting && !isInRoom
    }

    protected override IEnumerator RunInvestigation(IEnumerator investigation)
    {
        StopNonSuspiciousSound();
        // Wait until we're not blocked, not in a room, and not getting unstuck
        while (isBlocked || isInRoom || isWaiting)
        {
            yield return new WaitForSeconds(0.5f);
        }
        //isWalkingBack = true;
        //rightFloor = false;
        print(investigation.ToString());
        yield return StartCoroutine(investigation);
        isInvestigating = false;

        // If there is no more investigation we disable the icons
        if (investigationQueue.Count == 0 && !hasSeenMovement)
        {
            hasActiveInvestigation = false;

            if (alertSpriteRenderer != null)
            {
                alertSpriteRenderer.enabled = false;
                fovLight.color = nonSuspiciousColorFOV;
            }
        }

        lastSuspiciousTime = Time.time;

        if (CanPlayNonSuspiciousSound() && !isNonSuspiciousSoundPlaying)
        {
            StartNonSuspiciousSound();
        }
    }

    // Start the investigation of the sound
    //public override void InvestigateSound(SoundDetection objectsound, bool replaceObject, float targetFloor)
    //{
    //    investigationQueue.Enqueue(InvestigateFallingObject(objectsound, replaceObject, targetFloor));      
    //}

    public IEnumerator ReturnRightFloor()
    {
        yield return StartCoroutine(npcMovementController.ReachFloor(currentFloorLevel, initialFloorLevel));//ReachFloor(initialFloorLevel));
        if (FloorLevel == initialFloorLevel)
        {
            //rightFloor = true;
        }

    }

    public IEnumerator Patrol()
    {
        if (patrolPoints.Length == 0 || nextPatrolPoint == null) yield break;   // If there is no patrolPoint
        isPatrolling = true;
        // Get movement direction
        Vector2 destination = new Vector2(nextPatrolPoint.Point.position.x, transform.position.y);

        currentPoint = null;
        //print("CURRENTFLOOR" + currentFloorLevel);
        //print("NEXTPOINT " + nextPatrolPoint.FloorLevel);
        yield return npcMovementController.ReachTarget(destination, currentFloorLevel, nextPatrolPoint.FloorLevel);

        // NPC has arrived to the patrol point
        currentPoint = nextPatrolPoint;
        yield return HandleWaiting(currentPoint);
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
        //bool findNextPoint = false;
        indexPatrolPoints++;
        if (indexPatrolPoints >= patrolPoints.Length)
        {
            indexPatrolPoints = 0;
        }
        nextPatrolPoint = patrolPoints[indexPatrolPoints];
    }

    protected IEnumerator HandleWaiting(PatrolPointData currentPoint)
    {
        isWaiting = true;
        if (currentPoint.PatrolPointType == PatrolPointType.Room)
        {
            // Case 1: NPC is not in a room and the room is not blocked
            if(!isInRoom && !IsRoomBlocked(currentPoint))
            {
                
                canSee = false;
                fovLight.enabled = false;
                doorOpenSoundEvent.Post(gameObject);
                animator.SetBool("EnterRoom", true);
                isInRoom = true;
                yield return new WaitForSeconds(0.5f);
                doorCloseSoundEvent.Post(gameObject);
                yield return new WaitForSeconds(currentPoint.WaitTime); // Waiting in the room

                // Check again if the room became blocked while waiting
                if (!IsRoomBlocked(currentPoint))
                {
                    doorOpenSoundEvent.Post(gameObject);
                    animator.SetTrigger("ExitRoom");
                    yield return new WaitForSeconds(0.5f);
                    doorCloseSoundEvent.Post(gameObject);
                    isInRoom = false;
                    canSee = true;
                    fovLight.enabled = true;
                    animator.SetBool("EnterRoom", false);
                }
                else
                {
                    // The NPC is stuck
                    StopNonSuspiciousSound();
                    uint playingID = npcLockedSoundEvent.Post(gameObject, (uint)AkCallbackType.AK_Marker, MarkerCallback);
                    //Animator roomAnimator = currentPoint.GetComponent<Animator>();
                    //if(roomAnimator != null)
                    //{
                    //    roomAnimator.SetBool("IsNPCBlocked", true);
                    //}
                    isWaiting = false;
                    isBlocked = true;
                    yield break;
                }
            }
            // Case 2: The NPC is not in a room and the room is blocked
            else if(!isInRoom && IsRoomBlocked(currentPoint))
            {
                yield return new WaitForSeconds(currentPoint.WaitTimeBlocked);
            }
            // Case 3: NPC is in a room and the room is blocked
            else if(isInRoom && IsRoomBlocked(currentPoint))
            {
                StopNonSuspiciousSound();
                uint playingID = npcLockedSoundEvent.Post(gameObject, (uint)AkCallbackType.AK_Marker, MarkerCallback);
                //Animator roomAnimator = currentPoint.GetComponent<Animator>();
                //if (roomAnimator != null)
                //{
                //    roomAnimator.SetBool("IsNPCBlocked", true);
                //}
                // The NPC is stuck
                isWaiting = false;
                isBlocked = true;
                yield break;
            }
        }
        else
        {
            //isWaiting = false;
            yield return new WaitForSeconds(currentPoint.WaitTime);
        }
        isWaiting = false;
        isPatrolling = false;

        //if (CanPlayNonSuspiciousSound() && !isNonSuspiciousSoundPlaying)
        //{
        //    StartNonSuspiciousSound();
        //}
        MoveToNextAvailablePatrolPoint();
    }

    private void MarkerCallback(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info)
    {
        if (in_type == AkCallbackType.AK_Marker)
        {
            AkMarkerCallbackInfo markerInfo = (AkMarkerCallbackInfo)in_info;
            //Debug.Log("Marker Triggered: " + markerInfo.strLabel);

            // Ici tu déclenches ton animation
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
        //isGettingUnstuck = true;
        // Animation of NPC coming out of the room
        isWaiting = true;
        isBlocked = false;

        StopNonSuspiciousSound();

        animator.SetTrigger("ExitRoom");
        yield return new WaitForSeconds(0.5f);
        canSee = true;
        fovLight.enabled = true;
        animator.SetBool("EnterRoom", false);
        isWaiting = false;
        //print("ISWAITING2 " + isWaiting); 
        isInRoom = false;
        isPatrolling = false;

        if (CanPlayNonSuspiciousSound()) //&& !isNonSuspiciousSoundPlaying)
        {
            StartNonSuspiciousSound();
        }
        // After getting unstuck, move to the next patrol point
        //isGettingUnstuck = false;
        MoveToNextAvailablePatrolPoint();
    }

    public override void ResetInitialState()
    {
        base.ResetInitialState();
        animator.Rebind();      
        fovLight.enabled = true;
        isWaiting = false;
        isInRoom = false;
        isPatrolling = false;
        isBlocked = false;
        canSee = true;
        currentPoint = null;
        returnToFloor = null;
        patrolling = null;
        indexPatrolPoints = 0;

        // Reset the ambient sound after resetting other state
        StopNonSuspiciousSound();
        lastSuspiciousTime = -nonSuspiciousSoundCooldown;
        isNonSuspiciousSoundPlaying = false;
        if (initialPatrolPoint != null)
        {
            nextPatrolPoint = initialPatrolPoint;
        }
        npcLockedSoundEvent.Stop(gameObject);

        fovLight.color = nonSuspiciousColorFOV;
        // Stop room animation
        //foreach(PatrolPointData patrolPoint in patrolPoints)
        //{
        //    if (patrolPoint.PatrolPointType == PatrolPointType.Room && patrolPoint.SpriteRenderer != null)
        //    {
        //        Animator roomAnimator = currentPoint.GetComponent<Animator>();
        //        if (roomAnimator != null)
        //        {
        //            roomAnimator.SetBool("IsNPCBlocked", false);
        //        }
        //    }
        //}

        //if (CanPlayNonSuspiciousSound())
        //{
        //    StartNonSuspiciousSound();
        //}
        //cageAnimator.Play("Idle", -1, 0f);
    }
}
