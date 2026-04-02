using System.Collections;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class EnemyBehaviour : MonoBehaviour
{
    // Enemy vision variables
    protected float fieldOfViewAngle;
    [SerializeField]  protected float detectionRadius;  // NPC detection radius
    [SerializeField] protected bool facingRight;        // Is the NPC Sprite facing right    
    [SerializeField] protected LayerMask detectObjectLayer;   // Layer of objects to be detected by the NPC    
    [SerializeField] protected LayerMask ignoreLayerDetectObject;   // Layer to ignore when raycasting to check if the view is blocked
    [SerializeField] protected LayerMask mirrorLayer;   // Layer of objects to be detected by the NPC
    [SerializeField] protected LayerMask ignoreLayerReflection;   // Layer to ignore when raycasting to check if the mirror reflection is blocked
    [SerializeField] protected GameObject player;

    // Variable manage suspicion of the NPC
    [SerializeField] protected float minSuspiciousRotation; // Minimum rotation change in degrees to trigger suspicion
    [SerializeField] protected float minSuspiciousPosition; // Minimum position change to trigger suspicion
    protected bool isCurrentlyObserving;                      // Is the NPC already watching an object moving

    // Enemy patrol variables
    [SerializeField] protected Transform[] patrolPoints;    // Points were the NPC patrol
    protected int indexPatrolPoints;    // Next index patrol point

    [SerializeField] protected float movementSpeed;

    SpriteRenderer spriteRenderer;

    [Header("Investigation Variables")]
    protected bool isInvestigating = false;
    protected AudioSource audioSource;
    [SerializeField] protected float surpriseWaitTime = 2f;
    [SerializeField] protected float investigationWaitTime = 3f;
    protected virtual void Start()
    {
        fieldOfViewAngle = 180f;
        detectionRadius = 10f;
        isCurrentlyObserving = false;

        indexPatrolPoints = 0;
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        player = GameObject.FindWithTag("Player");
    }
    protected virtual void Update()
    {
        if (!isInvestigating)
        {
            Patrol();            
        }
        DetectMovingObjects();
        CheckMirrorReflection();

    }

    // Movement detection of the NPC
    protected virtual void DetectMovingObjects()
    {
        bool isObjectMoving = false;
        float objectSize = 0f;

        // Find all the possible possessed object in the room
        Collider2D[] objects = Physics2D.OverlapCircleAll(transform.position, detectionRadius,detectObjectLayer);
        foreach (Collider2D obj in objects)
        {            

            if(IsObjectInFieldOfView(obj))
            {
                // Check if there is no object blocking the sight of the NPC
                RaycastHit2D hit = Physics2D.Raycast(transform.position, (obj.transform.position - transform.position).normalized, detectionRadius, ~ignoreLayerDetectObject);
                
                // Is the path from the npc to the object clear?
                if (hit.collider != null && hit.collider == obj)
                {
                    // Get object size
                    Renderer objRenderer = obj.GetComponent<Renderer>();
                    objectSize = Mathf.Max(objRenderer.bounds.size.x, objRenderer.bounds.size.y);

                    // Check if the object is moving
                    PossessionController possessedObject = obj.GetComponent<PossessionController>();

                    if(possessedObject != null)
                    {
                        // Check if the object is moving in front of him
                        if (possessedObject.IsMoving)
                        {
                            isObjectMoving = true;
                        }
                        else
                        {
                            // Check if the object has changed significantly of position and rotation
                            float positionChange = Vector2.Distance(possessedObject.LastKnownPosition, obj.transform.position);
                            float rotationChange = Quaternion.Angle(possessedObject.LastKnownRotation, obj.transform.rotation);

                            // The object moved or rotated too much out of sight of the NPC
                            if (positionChange >= minSuspiciousPosition || rotationChange >= minSuspiciousRotation)
                            {
                                //possessedObject.UpdateLastKnownPositionRotation();
                                SuspicionManager.Instance.UpdateDisplacementSuspicion(objectSize, rotationChange, positionChange);
                            }
                        }
                        // Update the new position and rotation of the object
                        possessedObject.UpdateLastKnownPositionRotation();
                    }
                    
                    
                }
            }
        }
        // If the NPC sees an object moving for the first time
        if(isObjectMoving && !isCurrentlyObserving)
        {
            isCurrentlyObserving = true;
            SuspicionManager.Instance.AddParanormalObserver();
        }
        // If the object has stopped moving
        else if(!isObjectMoving && isCurrentlyObserving)
        {
            isCurrentlyObserving = false;
            SuspicionManager.Instance.RemoveParanormalObserver();
        }
        // If the object is still moving
        if(isObjectMoving && isCurrentlyObserving)
        {
            SuspicionManager.Instance.UpdateMovementSuspicion(objectSize);
        }
        
    }

    // Check if we can see the player trough the mirror
    protected void CheckMirrorReflection()
    {
        Collider2D[] mirrors = Physics2D.OverlapCircleAll(transform.position, detectionRadius, mirrorLayer);
        foreach(Collider2D mirrorCollider in mirrors)
        {
            Mirror mirror = mirrorCollider.GetComponentInParent<Mirror>();//GetComponent<Mirror>();
            if (mirror == null) continue;
            
            // Check if the mirror is in the field of view
            if (!IsObjectInFieldOfView(mirrorCollider)) continue;

            if (mirror.IsReflectedInMirror(player.GetComponent<Collider2D>()))
            {
                //if (!mirror.IsMirrorReflectionBlocked(player.GetComponent<Collider2D>()))
                //{
                //    print("DIE");
                //}

            }
        }
    }

    protected bool IsObjectInFieldOfView(Collider2D obj)
    {
        // Check if the object is in the line of sight of the NPC
        Vector2 directionToObject = (obj.transform.position - transform.position).normalized;
        float angle = Vector2.Angle(facingRight ? Vector2.right : Vector2.left, directionToObject);
        return angle <= fieldOfViewAngle / 2;
    }

    protected void Patrol()
    {
        // Get movement direction
        Vector2 destination = new Vector2(patrolPoints[indexPatrolPoints].position.x, transform.position.y);
        Vector2 direction = (destination - (Vector2)transform.position).normalized;

        // Flip sprite based on direction
        
        spriteRenderer.flipX = direction.x < 0;
        facingRight = !spriteRenderer.flipX;

        // Move towards destination
        transform.position = Vector2.MoveTowards(transform.position, destination, movementSpeed * Time.deltaTime);

        // If the enemy reaches his destination, he is given a new destination to patrol
        if (Mathf.Abs(patrolPoints[indexPatrolPoints].position.x - transform.position.x) <= 0.2f)
        {
            indexPatrolPoints++;
            if(indexPatrolPoints >= patrolPoints.Length)
            {
                indexPatrolPoints = 0;
            }
        }
    }
    // Start the investigation of the sound
    public void InvestigateSound(GameObject objectsound, bool replaceObject)
    {
        isInvestigating = true;
        StopAllCoroutines();
        StartCoroutine(InvestigateFallingObject(objectsound, replaceObject));
    }
    // Enemy behaviour for the investigation
    protected IEnumerator InvestigateFallingObject(GameObject objectsound, bool replaceObject)
    {
        // Take a surprise pause before going on investigation
        audioSource.Play();
        yield return new WaitForSeconds(surpriseWaitTime);

        

        // Go towards the sound
        Vector2 destination = new Vector2(objectsound.transform.position.x, transform.position.y);
        // Sprite face the right direction
        Vector2 direction = (destination - (Vector2)transform.position).normalized;
        spriteRenderer.flipX = direction.x < 0;

        while (Mathf.Abs(transform.position.x - objectsound.transform.position.x) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(transform.position, destination, movementSpeed*Time.deltaTime);
            yield return null;
        }

        // One NPC must replace the object to it's initial position
        FallingObject fallingObject = objectsound.GetComponent<FallingObject>();
        if (fallingObject != null && replaceObject)
        {
            //fallingObject.ReplaceObject();
            // Animation ICI!
            //fallingObject.FinishReplacement();
        }
        // Wait a bit of time before going back to normal
        yield return new WaitForSeconds(investigationWaitTime);
        isInvestigating = false;

    }

    // Debug method only
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

}
