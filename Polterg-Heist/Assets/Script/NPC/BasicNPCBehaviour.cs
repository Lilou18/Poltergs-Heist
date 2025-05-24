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
    [SerializeField] protected Color nonSuspiciousColorFOV;//"00FF1A";
    [SerializeField] protected Color alertColorFOV;
    protected Quaternion initialFOVRotation;

    [Header("NPC global variables")]
    [SerializeField] protected float currentFloorLevel;   // Floor where the npc is located
    [SerializeField] protected SpriteRenderer alertSpriteRenderer;
    [SerializeField] protected Sprite alertIcon;
    protected float initialFloorLevel;
    protected NPCMovementController npcMovementController;
    protected SpriteRenderer npcSpriteRenderer;
    protected Animator npcAnim;
    protected Animator npcAnimMouth;



    // Initial variables
    protected Vector3 initialPosition;  // Initial position of the NPC
    protected Quaternion initialRotation;
    protected bool initialFacingRight;  // He's he facing right or left

    [Header("NPC sound variables")]
    [SerializeField] protected AK.Wwise.Event surpriseSoundEvent;
    [SerializeField] protected AK.Wwise.Event nonSuspiciousSoundEvent;
    [SerializeField] protected float soundCooldown = 1.5f;  // Cooldown between sound of surprise
    protected float lastSoundTime;
    protected GameObject lastMovingObject;
    protected bool soundHasPlayed;

    protected float nonSuspiciousSoundCooldown = 0f;//3f;  // Cooldown before restarting ambient sound
    protected float lastSuspiciousTime;  // Last time something suspicious happened
    protected bool isNonSuspiciousSoundPlaying = false;  // Is the ambient sound currently playing
    protected Coroutine nonSuspiciousSoundCoroutine = null;


    //Animation variables
    [HideInInspector] public float directionX { get; set; }

    // Getters
    public float FloorLevel { get { return currentFloorLevel; } }

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
        //npcSpriteRenderer = GetComponent<SpriteRenderer>();
        npcMovementController = GetComponent<NPCMovementController>();
        if (TryGetComponent<Cat>(out Cat cat)) { npcAnim = GetComponentInChildren<Animator>(); }
        else 
        {
            npcAnim = GetComponentInChildren<Animator>();
            npcAnimMouth = npcAnim.transform.GetChild(0).GetComponentInChildren<Animator>();

        }
        //print(this.gameObject.name);
        //print("ANIM" + npcAnim.gameObject.name);
        fieldOfView = transform.GetChild(0).gameObject;
        fovLight = GetComponentInChildren<Light2D>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialFacingRight = facingRight;//!npcSpriteRenderer.flipX;
        initialFloorLevel = currentFloorLevel;
        directionX = 0;

        // Initialize sound tracking variables
        lastMovingObject = null;
        soundHasPlayed = false;
        lastSoundTime = -soundCooldown;

        lastSuspiciousTime = -nonSuspiciousSoundCooldown;

        initialFOVRotation = fovLight.transform.rotation;
    }
    protected virtual void Update()
    {
        DetectMovingObjects();
    }

    // Movement detection of the NPC
    protected virtual void DetectMovingObjects()
    {
        bool wasObjectMoving = isObjectMoving;
        isObjectMoving = false;
        float objectSize = 0f;
        bool foundMovingObject = false;
        GameObject currentMovingObject = null;

        // Find all the possible possessed object in the room
        Collider2D[] objects = Physics2D.OverlapCircleAll(transform.position, detectionRadius, detectObjectLayer);
        foreach (Collider2D obj in objects)
        {
            if(obj == null) continue;

            if (this.IsObjectInFieldOfView(obj))
            {
                //print("NOM DE L'OBJET " + obj.gameObject.name);
                // Check if there is no object blocking the sight of the NPC
                RaycastHit2D hit = Physics2D.Raycast(transform.position, (obj.transform.position - transform.position).normalized, detectionRadius, ~ignoreLayerSightBlocked);

                // Is the path from the npc to the object clear?
                if (hit.collider != null && hit.collider == obj)
                {
                    // Get object size
                    //Renderer objRenderer = obj.GetComponent<Renderer>();
                    //print("NOM DE L'OBJET " + obj.gameObject.name);
                    Renderer objRenderer;
                    objRenderer = obj.GetComponentInChildren<Renderer>();
                    /*if (obj.transform.GetChild(0).GetChild(0).TryGetComponent<Renderer>(out objRenderer)) { }
                    else if (obj.transform.GetChild(0).TryGetComponent<Renderer>(out objRenderer)) { }
                    else if (obj.TryGetComponent<Renderer>(out objRenderer)) { }*/
                    
                    objectSize = Mathf.Max(objRenderer.bounds.size.x, objRenderer.bounds.size.y);

                    // Check if the object is moving
                    PossessionController possessedObject = obj.GetComponent<PossessionController>();

                    if (possessedObject != null)
                    {
                        //print("NON NULLLL");
                        // Check if the object is moving in front of him
                        if (possessedObject.IsMoving)
                        {
                            if(!wasObjectMoving)
                            {
                                StopNonSuspiciousSound();
                            }
                            print(possessedObject.gameObject.name);
                            isObjectMoving = true;
                            foundMovingObject = true;
                            currentMovingObject = possessedObject.gameObject;

                            //if(alertIcon != null)
                            //{
                            //    alertIcon.enabled = true;
                            //}

                            HandleSoundEvent(currentMovingObject);
                            //soundEvent.Post(gameObject);
                        }

                        //HandleChangedPositionSuspicion(possessedObject, objectSize);
                    }


                }
                else if(hit.collider != null)
                {
                    //print(hit.collider.gameObject.name);
                }
            }
        }
        // Object stopped moving
        if(!foundMovingObject && lastMovingObject != null)
        {
            //lastMovingObject = null;
            soundHasPlayed = false;

            //if(alertIcon != null && isObjectMoving == false)
            //{
            //    alertIcon.enabled = false;
            //}
        }

        //if(wasObjectMoving && !isObjectMoving)
        //{
        //    StartNonSuspiciousSound();
        //}
        //else if(!wasObjectMoving && !isObjectMoving && !isNonSuspiciousSoundPlaying)
        //{
        //    StartNonSuspiciousSound();
        //}

        HandleMovementSuspicion(objectSize);
    }
    protected virtual void HandleChangedPositionSuspicion(PossessionController possessedObject, float objectSize)
    {
        // Empty default implementation
        // Will be override in HumanNPCBehaviour
    }

    protected virtual void HandleMovementSuspicion(float objectSize)
    {
        // Empty default implementation
        // Will be override in HumanNPCBehaviour
    }

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
        //directionX = facingRight ? -1f : 1f;

        if (GetComponentInChildren<NPCSpriteManager>() == null)
        {
            Vector3 rotationDegrees = fieldOfView.transform.eulerAngles;
            float newZ = facingRight ? -90f : 90f;
            fieldOfView.transform.localRotation = Quaternion.Euler(0, 0, newZ);
        }

        //Vector3 rotationDegrees = fieldOfView.transform.eulerAngles;
        //float newZ = facingRight ? -90f : 90f;
        //fieldOfView.transform.localRotation = Quaternion.Euler(0, 0, newZ);

        //float currentZ = Mathf.Round(rotationDegrees.z);
        //float newZ = currentZ == 90 ? -90 : 90;
        //rotationDegrees.z = -rotationDegrees.z;
    }

    // Manage the sound made by the NPC when he sees an object moving
    protected virtual void HandleSoundEvent(GameObject currentMovingObject)
    {
        // Check if we are past the cooldown
        bool cooldownElapsed = (Time.time - lastSoundTime) >= soundCooldown;
        //print(cooldownElapsed);
        // Case 1: Different object than before - play sound if cooldown has elapsed
        bool isDifferentObject = lastMovingObject != currentMovingObject;

        // If it's a different object than the last one we tracked, play the sound
        if (lastMovingObject != currentMovingObject)
        {
            surpriseSoundEvent.Post(gameObject);
            npcAnimMouth.SetTrigger("IsSurprised");
            lastMovingObject = currentMovingObject;
            soundHasPlayed = true;
            lastSoundTime = Time.time;
        }
        // If it's the same object but it had stopped and started again, play the sound
        else if(lastMovingObject == currentMovingObject && !soundHasPlayed && cooldownElapsed)
        {
            surpriseSoundEvent.Post(gameObject);
            npcAnimMouth.SetTrigger("IsSurprised");
            soundHasPlayed = true;
            lastSoundTime = Time.time;
        }
        // Otherwise, it's the same object still moving, so don't play the sound again
    }

    protected virtual void StartNonSuspiciousSound()
    {
        if(nonSuspiciousSoundCoroutine != null)
        {
            return;
        }
        nonSuspiciousSoundCoroutine = StartCoroutine(PlayNonSuspiciousSound());
        //if(!isNonSuspiciousSoundPlaying && nonSuspiciousSoundCoroutine == null)
        //{
        //    nonSuspiciousSoundCoroutine = StartCoroutine(PlayNonSuspiciousSound());
        //}
    }

    protected virtual IEnumerator PlayNonSuspiciousSound()
    {
        // Wait for the cooldown period
        float timeToWait = Mathf.Max(0, (lastSuspiciousTime + nonSuspiciousSoundCooldown) - Time.time);
        if (timeToWait > 0)
        {
            yield return new WaitForSeconds(timeToWait);
        }

        // Check if we should still play the sound (nothing happened during the waiting time)
        if (CanPlayNonSuspiciousSound())
        {
            if(nonSuspiciousSoundEvent != null)
            {
                nonSuspiciousSoundEvent.Post(gameObject);
                //alertIcon.enabled = false;
            }
            isNonSuspiciousSoundPlaying = true;
        }
        else
        {
            nonSuspiciousSoundCoroutine = null;
        }


    }

    protected virtual void StopNonSuspiciousSound()
    {
        if (isNonSuspiciousSoundPlaying && nonSuspiciousSoundEvent != null)
        {
            nonSuspiciousSoundEvent.Stop(gameObject);
            isNonSuspiciousSoundPlaying = false;
        }

        if (nonSuspiciousSoundCoroutine != null)
        {
            StopCoroutine(nonSuspiciousSoundCoroutine);
            nonSuspiciousSoundCoroutine = null;
        }

        lastSuspiciousTime = Time.time;
    }

    protected virtual bool CanPlayNonSuspiciousSound()
    {
        // Base condition - no moving objects
        return !isObjectMoving;
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
        //npcSpriteRenderer.flipX = !facingRight;
        currentFloorLevel = initialFloorLevel;
        // Reset icon movement detection
        if(alertSpriteRenderer != null)
        {
            alertSpriteRenderer.enabled = false;
        }
        fovLight.color = nonSuspiciousColorFOV;
        Vector3 rotationDegrees = fieldOfView.transform.eulerAngles;
        rotationDegrees.z = facingRight ? -90f : 90f;
        fieldOfView.transform.eulerAngles = rotationDegrees;
        fovLight.transform.rotation = initialFOVRotation;
        lastMovingObject = null;
        soundHasPlayed = false;

        StopNonSuspiciousSound();
        lastSuspiciousTime = -nonSuspiciousSoundCooldown;
        isNonSuspiciousSoundPlaying = false;
        fovLight.color = nonSuspiciousColorFOV;
        StopAllCoroutines();
        //StartNonSuspiciousSound();
    }
}
