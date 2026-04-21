using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;
public abstract class BasicNPCBehaviour : MonoBehaviour, IResetInitialState
{
    // Base calss for all NPCs
    // Handles:
    // - Vision (field of view, detection)
    // - Movement detection (possessed objects)
    // - Initial state reset
    // - Icon feedback via NPCIconDisplay


    [Header("Field of view")]
    [SerializeField] protected float detectionRadius = 10f;         // NPC detection radius
    [SerializeField] protected bool facingRight;                    // Is the NPC Sprite facing right    
    [SerializeField] protected LayerMask detectObjectLayer;         // Layers containing detectable objects by the NPC  
    [SerializeField] protected LayerMask ignoreLayerSightBlocked;   // Layers ignored when checking line of sight

    protected float fieldOfViewAngle = 180f;                        // Vision cone angle
    protected bool isObjectMoving;                                  // True if a moving possessed object is currently detected
    protected bool isCurrentlyObserving;                            // True if the NPC is actively watching a moving object 

    protected GameObject fieldOfView;                               // Visual cone object
    protected Light2D fovLight;                                     // Light representing vision cone
    protected Quaternion initialFOVRotation;                        // Initial rotation of the FOV light

    // Components
    protected NPCMovementController npcMovementController;          // Handles NPC movement logic
    protected SpriteRenderer npcSpriteRenderer;                     // NPC sprite renderer
    protected Animator npcAnim;                                     // NPC Animator
    protected NPCIconDisplay iconDisplay;                           // Handles UI icon feedback

    [Header("NPC State")]
    [SerializeField] protected float currentFloorLevel;             // Current floor level of the NPC

    protected float initialFloorLevel;                              // Initial floor level
    protected Vector3 initialPosition;                              // Initial NPC position
    protected Quaternion initialRotation;                           // Initial NPC rotation
    protected bool initialFacingRight;                              // Initial facing direction

    [Header("NPC sound variables")]
    [SerializeField] protected AK.Wwise.Event surpriseSoundEvent;   // Played when NPC detects something unexpected

    // Getters
    public float FloorLevel { get { return currentFloorLevel; } }

    public float InitialFloorLevel => initialFloorLevel;
    public Vector3 InitialPosition => initialPosition;

    public SpriteRenderer SpriteRenderer { get { return npcSpriteRenderer; } }
    public NPCMovementController NpcMovementController { get { return npcMovementController; } }
    public GameObject FieldOfView { get { return fieldOfView; } }

    //Getters and Setters
    public bool FacingRight {  get { return facingRight; } set { facingRight = value; } }
    public bool InitialFacingRight { get { return initialFacingRight; } }

    protected void Awake()
    {
        npcSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    protected virtual void Start()
    {
        // Initialize detection state
        isCurrentlyObserving = false;
        isObjectMoving = false;

        npcMovementController = GetComponent<NPCMovementController>();
        npcAnim = GetComponentInChildren<Animator>();
        fieldOfView = transform.GetChild(0).gameObject;
        fovLight = GetComponentInChildren<Light2D>();
        iconDisplay = GetComponent<NPCIconDisplay>();

        // Save initial state
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialFacingRight = facingRight;
        initialFloorLevel = currentFloorLevel;
        initialFOVRotation = fovLight.transform.rotation;
    }
    protected virtual void Update()
    {
        DetectMovingObjects();
        UpdateIconDisplay();
    }

    // Runs a scan each frame and dispatches the result to OnDetectionResult().
    protected virtual void DetectMovingObjects()
    {
        DetectionResult result = ScanForMovingObject();
        isObjectMoving = result.foundMovingObject;
        OnDetectionResult(result);
    }

    // Scans all objects in detection radius and checks:
    // 1. Is the object within the FOV cone angle?
    // 2. Is the line of sight blocked by an object?
    // 3. Is the object a possessed object that is currently moving?
    // Returns a DetectionResult summarizing what was found.
    protected DetectionResult ScanForMovingObject()
    {
        bool wasMoving = isObjectMoving;
        bool foundMovingObject = false;
        GameObject currentMovingObject = null;
        float objectSize = 0f;
        float objectWidth = 0f;
        float objectHeight = 0f;

        // Find all the possible possessed object in the NPC radius
        Collider2D[] objects = Physics2D.OverlapCircleAll(transform.position, detectionRadius, detectObjectLayer);
        foreach (Collider2D obj in objects)
        {
            if (obj == null) continue;

            // Skip objects outside the FOV cone
            if (!IsObjectInFieldOfView(obj)) continue;

            // Skip objects with a blocked line of sight
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                (obj.transform.position - transform.position).normalized,
                detectionRadius,
                ~ignoreLayerSightBlocked);

            if (hit.collider == null || hit.collider != obj) continue;

            // Measure object size for suspicion scaling
            Renderer objRenderer = obj.GetComponentInChildren<Renderer>();
            objectWidth = objRenderer.bounds.size.x;
            objectHeight = objRenderer.bounds.size.y;
            objectSize = Mathf.Max(objectWidth, objectHeight);


            // Is the object actively being moved by the player
            PossessionController possessedObject = obj.GetComponent<PossessionController>();
            if (possessedObject != null && possessedObject.IsMoving)
            {
                foundMovingObject = true;
                currentMovingObject = possessedObject.gameObject;
            }
        }

        return new DetectionResult(foundMovingObject, wasMoving, currentMovingObject, objectSize, objectWidth, objectHeight);
    }

    // Override this in child classes to define behavior when detection changes.
    protected virtual void OnDetectionResult(DetectionResult result) { }

    // Returns true if the object is within the NPC's FOV.
    protected virtual bool IsObjectInFieldOfView(Collider2D obj)
    {
        Vector2 directionToObject = (obj.transform.position - transform.position).normalized;
        float angle = Vector2.Angle(facingRight ? Vector2.right : Vector2.left, directionToObject);
        return angle <= fieldOfViewAngle / 2;
    }

    // Updates the NPC's tracked floor level.
    public void UpdateFloorLevel(float currenrFloorLevel)
    {
        currentFloorLevel = currenrFloorLevel;
    }

    // Sets the NPC facing direction
    public void SetFacingDirection(bool faceRight)
    {
        facingRight = faceRight;
    }

    // Calls iconDisplay to display the current icon state.
    protected void UpdateIconDisplay()
    {
        if (iconDisplay != null)
            iconDisplay.UpdateIcon(GetIconState());
    }

    // Returns the icon state to display above the NPC.
    // Override in subclasses to provide class-specific logic.
    protected virtual IconState GetIconState()
    {
        return IconState.None;
    }

    // Debug method only
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    // Restores the NPC to its initial state at scene start.
    public virtual void ResetInitialState()
    {
        this.transform.position = initialPosition;
        this.transform.rotation = initialRotation;
        facingRight = initialFacingRight;
        currentFloorLevel = initialFloorLevel;

        Vector3 rotationDegrees = fieldOfView.transform.eulerAngles;
        rotationDegrees.z = facingRight ? -90f : 90f;
        fieldOfView.transform.eulerAngles = rotationDegrees;
        fovLight.transform.rotation = initialFOVRotation;

        if (iconDisplay != null)
            iconDisplay.Reset();
        StopAllCoroutines();
    }
}

// Result of each detection scan made each frame
public readonly struct DetectionResult
{
    public readonly bool foundMovingObject;     // True if a moving possessed object was detected this frame
    public readonly bool wasAlreadyMoving;      // True if a moving object was detected last frame
    public readonly GameObject movingObject;    // The moving object detected
    public readonly float objectSize;           // Largest dimension of the detected object (used for suspicion scaling)
    public readonly float objectWidth;          // Width of the detected object
    public readonly float objectHeight;         // Height of the detected object

    public DetectionResult(
        bool isObjectMoving,
        bool wasMoving,
        GameObject obj,
        float size,
        float width = 0f,
        float height = 0f)
    {
        foundMovingObject = isObjectMoving;
        wasAlreadyMoving = wasMoving;
        movingObject = obj;
        objectSize = size;
        objectWidth = width;
        objectHeight = height;
    }
}
