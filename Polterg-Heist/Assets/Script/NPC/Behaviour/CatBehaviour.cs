using UnityEngine;
using System.Collections;

public class Cat : BasicNPCBehaviour, IPatrol
{
    // Sound variables
    [SerializeField] protected AK.Wwise.Event catSlapEvent;
    [SerializeField] protected AK.Wwise.Event catSoundsEvent;
    [SerializeField] protected AK.Wwise.Event cageCloseSoundEvent;
    protected bool isNormalCat = true;

    [Header("Patrolling Variables")]  
    [SerializeField] PatrolPointData[] patrolPoints;  // All cat patrol destinations
    PatrolPointData nextPatrolPoint;  // Next cat patrol destination
    private int indexPatrolPoints;    // Keep track of patrol points
    private bool canMove;
    private PatrolPointData initialPatrolPoint;

    [Header("Hunting variables")]
    [SerializeField] float attackTime = 3f;
    [SerializeField] float huntingSpeed = 8f;
    [SerializeField] float maxHeightObject = 0.6f;  // Maximum height of an object that the cat can chase
    [SerializeField] float maxWidthObject = 0.6f;   //Maximum width of an object thtat the cat can chase
    public bool isHunting;   // Is the cat chasing an objet
    private bool isAttacking;   // Is the cat attacking the object
    GameObject targetPossessedObject; // Cat hunting target

    Animator catAnim;
    Collider2D catCollider;
    Coroutine patrolCoroutine;



    GameObject cage;    // Cage the cat is trapped in

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

        isNormalCat = true;
        catSoundsEvent.Post(gameObject);
    }

    protected override void Update()
    {
        UpdateIconDisplay();

        if (canMove)
        {
            DetectMovingObjects();
            
            // Patrolling cat
            if (!isHunting && !isAttacking && patrolCoroutine == null)
            {
                if (!isNormalCat)
                {
                    isNormalCat = true;
                    catSoundsEvent.Post(gameObject);
                }                
                patrolCoroutine = StartCoroutine(Patrol());
            }
            // Hunting Cat
            else if (isHunting && targetPossessedObject != null && !isAttacking)
            {
                isNormalCat = false;
                catSoundsEvent.Stop(gameObject);

                if (patrolCoroutine != null)
                {
                    StopCoroutine(patrolCoroutine);
                    patrolCoroutine = null;
                }

                StartCoroutine(ObjectHunting());
            }
        }        
    }

    protected override IconState GetIconState()
    {
        if (!canMove) return IconState.None;
        if (isHunting || isAttacking) return IconState.Alert;
        return IconState.None;
    }

    // Cat movement detection
    protected override void OnDetectionResult(DetectionResult result)
    {
        if (result.foundMovingObject && !isHunting && result.objectWidth <= maxWidthObject && result.objectHeight <= maxHeightObject)
        {
            isHunting = true;
            targetPossessedObject = result.movingObject;
        }
    }
    
    // Patrolling of the cat
    public IEnumerator Patrol()
    {
        if (patrolPoints.Length == 0 || nextPatrolPoint == null) yield break;   // If there is no patrolPoint

        // Get movement direction
        Vector3 destination = new Vector3(nextPatrolPoint.Point.position.x, transform.position.y, transform.position.z);
        yield return npcMovementController.ReachTarget(destination, currentFloorLevel, nextPatrolPoint.FloorLevel);
        patrolCoroutine = null;
        MoveToNextAvailablePatrolPoint();
    }

    // Which patrol point is the new destination of the cat
    public void MoveToNextAvailablePatrolPoint()
    {
        indexPatrolPoints++;
        if (indexPatrolPoints >= patrolPoints.Length)
        {
            indexPatrolPoints = 0;
        }
        nextPatrolPoint = patrolPoints[indexPatrolPoints];
    }

    // The cat must chase any object it sees moving
    private IEnumerator ObjectHunting()
    {
        isAttacking = true;        
        surpriseSoundEvent.Post(gameObject);

        // Continue hunting until the cat catches the object or loses track of it
        while (isHunting && targetPossessedObject != null)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position,
                                     (targetPossessedObject.transform.position - transform.position).normalized,
                                     detectionRadius,
                                     ~ignoreLayerSightBlocked);

            // Check if the possessed object is still in the field of view of the cat
            if (hit.collider == null || hit.collider.gameObject != targetPossessedObject)
            {
                isHunting = false;
                isAttacking = false;
                targetPossessedObject = null;
                yield break;
            }

            // Get the possessed object's position and movement
            Vector3 objectPosition = targetPossessedObject.transform.position;
            PossessionController possessionController = targetPossessedObject.GetComponent<PossessionController>();

            // Check if cat is directly beneath the object
            bool isCatUnderObject = Mathf.Abs(transform.position.x - objectPosition.x) < 0.5f;

            if (isCatUnderObject && objectPosition.y > transform.position.y)
            {
                // If directly under and object is higher, use object's movement direction
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
                // Regular behavior - move toward object
                Vector3 destination = new Vector3(objectPosition.x, transform.position.y, objectPosition.z);
                Vector2 direction = (new Vector2(destination.x, destination.y) - (Vector2)transform.position).normalized;
                bool faceRight = direction.x >= 0;
                SetFacingDirection(faceRight);
            }

            // Cat is running towards the object target            
            transform.position = new Vector3(transform.position.x, transform.position.y, objectPosition.z);

            Vector3 moveDestination = new Vector3(objectPosition.x, transform.position.y, objectPosition.z);
            transform.position = Vector3.MoveTowards(transform.position, moveDestination, huntingSpeed * Time.deltaTime);
            // Verify if the Cat toutched the object

            Bounds catBounds = catCollider.bounds;
            Bounds targetBounds = targetPossessedObject.GetComponent<Collider2D>().bounds;

            // Check if bounds overlap in X and Y axes only
            bool xOverlap = Mathf.Max(catBounds.min.x, targetBounds.min.x) <= Mathf.Min(catBounds.max.x, targetBounds.max.x);
            bool yOverlap = Mathf.Max(catBounds.min.y, targetBounds.min.y) <= Mathf.Min(catBounds.max.y, targetBounds.max.y);

            if (xOverlap && yOverlap)
            {
                isHunting = false;
                yield return AttackObject();
                break;
            }
            yield return null;
        }
        
    }

    // The cat attack the possessed object
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
        targetObjectManager.isAttacked = true;
        targetObjectManager.LockPossession(true);   // The player can't possessed this object as long as the cat attack it
        // After the attack the object is no longer a target
        targetPossessedObject = null;
        if (targetObjectManager.IsPossessing)
        {
            targetObjectManager.StopPossession();
        }


        yield return new WaitForSeconds(attackTime);      
        
        targetObjectManager.LockPossession(false);
        catAnim.SetBool("IsAttacking", false);
        isAttacking = false;

        targetObjectManager.isAttacked = false;
    }

    // If the cat enters the cage it remains trapped.
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (canMove && collision.gameObject.CompareTag("Cage"))
        {
            
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
        // Reset Sound variables
        isNormalCat = true;
        catSlapEvent.Stop(gameObject);
        catSoundsEvent.Post(gameObject);
    }
}
