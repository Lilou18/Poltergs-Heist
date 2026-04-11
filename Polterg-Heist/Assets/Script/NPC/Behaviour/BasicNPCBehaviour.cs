using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;
public abstract class BasicNPCBehaviour : MonoBehaviour, IResetInitialState
{
    // NPC vision variables
    [Header("Field of view")]
    [SerializeField] protected float detectionRadius = 10f;  // NPC detection radius
    [SerializeField] protected bool facingRight;        // Is the NPC Sprite facing right    
    [SerializeField] protected LayerMask detectObjectLayer;   // Layer of objects to be detected by the NPC    
    [SerializeField] protected LayerMask ignoreLayerSightBlocked;   // Layer to ignore when raycasting to check if the view is blocked
    protected bool isObjectMoving;    // Is there an object moving in front of him?
    protected bool isCurrentlyObserving;    // Is the NPC already watching an object moving?
    protected float fieldOfViewAngle;
    protected GameObject fieldOfView;
    protected Light2D fovLight;
    protected Quaternion initialFOVRotation;

    [Header("NPC global variables")]
    [SerializeField] protected float currentFloorLevel;   // Floor where the npc is located
    protected float initialFloorLevel;
    protected NPCMovementController npcMovementController;
    protected SpriteRenderer npcSpriteRenderer;
    protected Animator npcAnim;

    protected NPCIconDisplay iconDisplay;

    // Initial variables
    protected Vector3 initialPosition;  // Initial position of the NPC
    protected Quaternion initialRotation;
    protected bool initialFacingRight;  // He's he facing right or left

    [Header("NPC sound variables")]
    [SerializeField] protected AK.Wwise.Event surpriseSoundEvent;

    //Animation variables
    [HideInInspector] public float directionX { get; set; }

    // Getters
    public float FloorLevel { get { return currentFloorLevel; } }

    public float InitialFloorLevel => initialFloorLevel;
    public Vector3 InitialPosition => initialPosition;

    public SpriteRenderer SpriteRenderer { get { return npcSpriteRenderer; } }
    public NPCMovementController NpcMovementController { get { return npcMovementController; } }
    public GameObject FieldOfView { get { return fieldOfView; } }

    //Getters and Setters
    public bool FacingRight {  get { return facingRight; } set { facingRight = value; } }
    public bool IniFacingRight { get { return initialFacingRight; } }

    protected virtual void Start()
    {
        fieldOfViewAngle = 180f;
        isCurrentlyObserving = false;
        isObjectMoving = false;

        npcSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        npcMovementController = GetComponent<NPCMovementController>();

        npcAnim = GetComponentInChildren<Animator>();


        fieldOfView = transform.GetChild(0).gameObject;
        fovLight = GetComponentInChildren<Light2D>();

        iconDisplay = GetComponent<NPCIconDisplay>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialFacingRight = facingRight;
        initialFloorLevel = currentFloorLevel;
        directionX = 0;

        initialFOVRotation = fovLight.transform.rotation;
    }
    protected virtual void Update()
    {
        DetectMovingObjects();
        UpdateIconDisplay();
    }

    protected void UpdateIconDisplay()
    {
        if (iconDisplay != null)
            iconDisplay.UpdateIcon(GetIconState());
    }
    protected virtual IconState GetIconState()
    {
        return IconState.None;
    }

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

            if (!IsObjectInFieldOfView(obj)) continue;

            // Check if there is no object blocking the sight of the NPC
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                (obj.transform.position - transform.position).normalized,
                detectionRadius,
                ~ignoreLayerSightBlocked);

            // Is the path from the npc to the object clear?
            if (hit.collider == null || hit.collider != obj) continue;

            Renderer objRenderer = obj.GetComponentInChildren<Renderer>();
            objectWidth = objRenderer.bounds.size.x;
            objectHeight = objRenderer.bounds.size.y;
            objectSize = Mathf.Max(objectWidth, objectHeight);

            // Check if the object is moving in front of him
            PossessionController possessedObject = obj.GetComponent<PossessionController>();
            if (possessedObject != null && possessedObject.IsMoving)
            {
                foundMovingObject = true;
                currentMovingObject = possessedObject.gameObject;
            }
        }

        return new DetectionResult(foundMovingObject, wasMoving, currentMovingObject, objectSize, objectWidth, objectHeight);
    }

    // Movement detection of the NPC
    protected virtual void DetectMovingObjects()
    {
        DetectionResult result = ScanForMovingObject();
        isObjectMoving = result.foundMovingObject;
        OnDetectionResult(result);
    }

    protected virtual void OnDetectionResult(DetectionResult result) { }

    protected virtual bool IsObjectInFieldOfView(Collider2D obj)
    {
        // Check if the object is in the line of sight of the NPC
        Vector2 directionToObject = (obj.transform.position - transform.position).normalized;
        float angle = Vector2.Angle(facingRight ? Vector2.right : Vector2.left, directionToObject);
        return angle <= fieldOfViewAngle / 2;
    }

    public void UpdateFloorLevel(float currenrFloorLevel)
    {
        currentFloorLevel = currenrFloorLevel;
    }

    public void FlipFieldOfView()
    {
        if (GetComponentInChildren<NPCSpriteManager>() == null)
        {
            Vector3 rotationDegrees = fieldOfView.transform.eulerAngles;
            float newZ = facingRight ? -90f : 90f;
            fieldOfView.transform.localRotation = Quaternion.Euler(0, 0, newZ);
        }       
    }

    // Debug method only
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

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

public readonly struct DetectionResult
{
    public readonly bool foundMovingObject;
    public readonly bool wasAlreadyMoving;  
    public readonly GameObject movingObject;
    public readonly float objectSize;
    public readonly float objectWidth;      
    public readonly float objectHeight;     

    public DetectionResult(
        bool found,
        bool wasMoving,
        GameObject obj,
        float size,
        float width = 0f,
        float height = 0f)
    {
        foundMovingObject = found;
        wasAlreadyMoving = wasMoving;
        movingObject = obj;
        objectSize = size;
        objectWidth = width;
        objectHeight = height;
    }
}
