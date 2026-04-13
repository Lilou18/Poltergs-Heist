using UnityEngine;
using System.Collections;

public class Cat : BasicNPCBehaviour, IPatrol
{
    // Extends BasicNPCBehaviour for the cat NPC.
    // Handles:
    // - Patrolling between patrol points
    // - Hunting possessed objects that enter its field of view
    // - Attacking possessed objects on contact
    // - Getting trapped in a cage

    [Header("Sound Event")]
    [SerializeField] protected AK.Wwise.Event catSlapEvent;         // Played when the cat attacks an object
    [SerializeField] protected AK.Wwise.Event catSoundsEvent;       // Idle sound, stopped during hunting
    [SerializeField] protected AK.Wwise.Event cageCloseSoundEvent;  // Played when the cage closes on the cat


    [Header("Patrolling")]  
    [SerializeField] PatrolPointData[] patrolPoints;                // Ordered list of patrol destinations    

    [Header("Hunting")]
    [SerializeField] float attackTime = 3f;                         // Duration of the attack before releasing the object
    [SerializeField] float huntingSpeed = 8f;                       // Movement speed while chasing a target
    [SerializeField] float maxHeightObject = 0.6f;                  // Maximum object height the cat will chase
    [SerializeField] float maxWidthObject = 0.6f;                   // Maximum object width the cat will chase

    private PatrolPointData nextPatrolPoint;                        // Next patrol destination
    private PatrolPointData initialPatrolPoint;                     // First patrol point
    private int indexPatrolPoints;                                  // Index of the next patrol point to visit


    private GameObject targetPossessedObject;                       // The possessed object the cat is currently hunting
    private GameObject cage;                                        // The cage the cat is currently trapped in, null if free

    private bool canMove;                                           // False when the cat is trapped in a cage
    protected bool isIdleSoundPlaying = true;                       // True while the idle sound is playing
    private bool isHunting;                                         // True while the cat is chasing a target
    private bool isAttacking;                                       // True while the cat is performing an attack


    private Animator catAnim;                                       // Controls attack and caught animations
    private Collider2D catCollider;                                 // Used for overlap detection when checking if the cat touched the target

    private Coroutine patrolCoroutine;                              // Currently running patrol coroutine
    private Coroutine huntingCoroutine;                             // Currently running hunting coroutine

    protected override void Start()
    {
        base.Start();
        indexPatrolPoints = 0;
        nextPatrolPoint = patrolPoints[0];
        initialPatrolPoint = nextPatrolPoint;
        isHunting = false;
        isAttacking = false;
        canMove = true;

        catCollider = GetComponent<Collider2D>();
        catAnim = GetComponentInChildren<Animator>();

        isIdleSoundPlaying = true;
        catSoundsEvent.Post(gameObject);
    }

    // Handles patrol and hunting state transitions each frame.
    // Update the cat Icon base on the state
    protected override void Update()
    {
        UpdateIconDisplay();

        if (canMove)
        {
            DetectMovingObjects();
            
            // If there is nothing out of ordinary, the cat patrol
            if (!isHunting && !isAttacking && patrolCoroutine == null)
            {
                if (!isIdleSoundPlaying)
                {
                    // Play idle sound if it was stopped during hunting
                    isIdleSoundPlaying = true;
                    catSoundsEvent.Post(gameObject);
                }                
                patrolCoroutine = StartCoroutine(Patrol());
            }
            // If a small enough possessed object move in the FOV of the cat,
            // we start the hunt
            else if (isHunting && targetPossessedObject != null && !isAttacking)
            {
                isIdleSoundPlaying = false;
                catSoundsEvent.Stop(gameObject);

                // Stop the patrol to start the hunt
                if (patrolCoroutine != null)
                {
                    StopCoroutine(patrolCoroutine);
                    patrolCoroutine = null;
                }

                if (huntingCoroutine == null)
                    huntingCoroutine = StartCoroutine(ObjectHunting());
            }
        }        
    }

    // Shows alert icon while hunting or attacking, hides it when trapped in a cage.
    protected override IconState GetIconState()
    {
        if (!canMove) return IconState.None;
        if (isHunting || isAttacking) return IconState.Alert;
        return IconState.None;
    }

    // Starts hunting if a small enough moving object enters the detection radius.
    // Objects larger than maxWidthObject or maxHeightObject are ignored.
    protected override void OnDetectionResult(DetectionResult result)
    {
        if (result.foundMovingObject && !isHunting && result.objectWidth <= maxWidthObject && result.objectHeight <= maxHeightObject)
        {
            isHunting = true;
            targetPossessedObject = result.movingObject;
        }
    }

    // Moves the cat to the next patrol point.
    public IEnumerator Patrol()
    {
        if (patrolPoints.Length == 0 || nextPatrolPoint == null) yield break;   // If there is no patrolPoint

        // Move to the patrol point
        Vector3 destination = new Vector3(nextPatrolPoint.Point.position.x, transform.position.y, transform.position.z);
        yield return npcMovementController.ReachTarget(destination, currentFloorLevel, nextPatrolPoint.FloorLevel);

        // Set up for the next patrol point
        patrolCoroutine = null;
        MoveToNextAvailablePatrolPoint();
    }

    // Advances to the next patrol point, then start again at the end of the list.
    public void MoveToNextAvailablePatrolPoint()
    {
        indexPatrolPoints++;
        if (indexPatrolPoints >= patrolPoints.Length)
        {
            indexPatrolPoints = 0;
        }
        nextPatrolPoint = patrolPoints[indexPatrolPoints];
    }

    // Chases the target possessed object until contact or loss of sight.
    // When there is a contact, the cat attack.
    // On loss of sight, resets hunting state to resume patrol.
    private IEnumerator ObjectHunting()
    {
        isAttacking = true;        
        surpriseSoundEvent.Post(gameObject);

        PossessionController possessionController = targetPossessedObject.GetComponent<PossessionController>();
        Collider2D targetCollider = targetPossessedObject.GetComponent<Collider2D>();

        // Continue hunting until the cat catches the object or loses track of it
        while (isHunting && targetPossessedObject != null)
        {
            // Verify if the target is still in the FOV
            RaycastHit2D hit = Physics2D.Raycast(transform.position,
                                     (targetPossessedObject.transform.position - transform.position).normalized,
                                     detectionRadius,
                                     ~ignoreLayerSightBlocked);

            // Target is no longer visible. Abort the hunt and resume patrol
            if (hit.collider == null || hit.collider.gameObject != targetPossessedObject)
            {
                isHunting = false;
                isAttacking = false;
                targetPossessedObject = null;
                huntingCoroutine = null;
                yield break;
            }

            // Get the possessed object's position and movement
            Vector3 objectPosition = targetPossessedObject.transform.position;

            // Special case: cat is directly below the object
            // Mirror the object's movement direction instead of chasing its position
            bool isCatUnderObject = Mathf.Abs(transform.position.x - objectPosition.x) < 0.5f;
            if (isCatUnderObject && objectPosition.y > transform.position.y)
            {
                if (possessionController != null)
                {
                    // Get movement direction from the possessed object
                    Vector2 objectDirection = possessionController.GetMovementDirection();
                    bool faceRight = objectDirection.x >= 0;

                    // Only update facing if the object is actually moving horizontally
                    if (possessionController.IsMoving)
                    {
                        SetFacingDirection(faceRight);
                    }
                }
            }
            else
            {
                // Standard chase. Face and move toward the target's X position
                Vector3 destination = new Vector3(objectPosition.x, transform.position.y, objectPosition.z);
                Vector2 direction = (new Vector2(destination.x, destination.y) - (Vector2)transform.position).normalized;
                bool faceRight = direction.x >= 0;
                SetFacingDirection(faceRight);
            }

            
            // Keep Z position aligned with the target to ensure correct depth sorting
            transform.position = new Vector3(transform.position.x, transform.position.y, objectPosition.z);

            // Cat is running towards the object target
            Vector3 moveDestination = new Vector3(objectPosition.x, transform.position.y, objectPosition.z);
            transform.position = Vector3.MoveTowards(transform.position, moveDestination, huntingSpeed * Time.deltaTime);
            
            // Verify if the Cat toutched the object
            Bounds catBounds = catCollider.bounds;
            Bounds targetBounds = targetCollider.bounds;

            // Check if bounds overlap in X and Y axes only
            bool xOverlap = Mathf.Max(catBounds.min.x, targetBounds.min.x) <= Mathf.Min(catBounds.max.x, targetBounds.max.x);
            bool yOverlap = Mathf.Max(catBounds.min.y, targetBounds.min.y) <= Mathf.Min(catBounds.max.y, targetBounds.max.y);

            // The cat is touching the object so it attacks and stop the hunt
            if (xOverlap && yOverlap)
            {
                isHunting = false;
                yield return AttackObject();
                break;
            }
            yield return null;
        }
        huntingCoroutine = null;
    }

    // Locks possession of the target object and waits for the attack to finish.
    // Releases the object after attackTime seconds.
    // The player is being forced out of the possessed object and can't
    // possessed this object until the attack is finished.
    private IEnumerator AttackObject()
    {
        isAttacking = true;       
        catSlapEvent.Post(gameObject);
        catAnim.SetBool("IsAttacking", true);

        PossessionManager targetObjectManager = targetPossessedObject.GetComponent<PossessionManager>();
        if (targetObjectManager == null)
        {
            Debug.Log("Error: The possessed object should have a PossessionManager component");
            isAttacking = false;
            yield break;
        }
        targetObjectManager.IsAttacked = true;

        // Prevent the player from possessing the object while the cat is attacking
        targetObjectManager.LockPossession(true);

        // After the attack, the object is no longer a target
        targetPossessedObject = null;

        // If the player is currently possessing the object, force a depossession
        if (targetObjectManager.IsPossessing)
        {
            targetObjectManager.StopPossession();
        }

        yield return new WaitForSeconds(attackTime);      
        
        // When the attack is finished the player can posssessed the object again
        targetObjectManager.LockPossession(false);
        catAnim.SetBool("IsAttacking", false);
        isAttacking = false;

        targetObjectManager.IsAttacked = false;
    }

    // Traps the cat when it is fully inside a cage trigger.
    // Stops all movement and plays the cage close animation.
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (canMove && collision.gameObject.CompareTag("Cage"))
        {
            // If the cat is completely inside the cage, it's trapped
            if(collision.bounds.Contains(catCollider.bounds.min) && collision.bounds.Contains(catCollider.bounds.max))
            {           
                surpriseSoundEvent.Post(gameObject);
                canMove = false;
                StopAllCoroutines();
                fovLight.enabled = false;

                collision.GetComponentInParent<Animator>().SetBool("CloseCage", true);
                cageCloseSoundEvent.Post(gameObject);
                catAnim.SetBool("IsAttacking", false);
                catAnim.SetBool("IsCaught", true);
                cage = collision.gameObject;
            }            
        }
    }

    // Clears the caught animation flag when the cat exits the cage trigger.
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Cage"))
        {
            catAnim.SetBool("IsCaught", false);
        }
    }

    public override void ResetInitialState()
    {
        base.ResetInitialState();
        patrolCoroutine = null;
        huntingCoroutine = null;
        isHunting = false;
        isAttacking = false;
        nextPatrolPoint = initialPatrolPoint;
        indexPatrolPoints = 0;
        canMove = true;
        fovLight.enabled = true;
        
        if(cage != null)
        {
            Animator cageAnimator = cage.GetComponentInParent<Animator>();
            cageAnimator.SetBool("CloseCage", false);
            cageAnimator.Play("Idle", -1, 0f);
        }

        catAnim.SetBool("IsAttacking", false);
        catAnim.SetBool("IsCaught", false);
        npcMovementController.Reset();
        isIdleSoundPlaying = true;
        catSlapEvent.Stop(gameObject);
        catSoundsEvent.Post(gameObject);
    }
}
