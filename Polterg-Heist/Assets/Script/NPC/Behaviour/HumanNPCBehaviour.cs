using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class HumanNPCBehaviour : BasicNPCBehaviour
{
    // Extends BasicNPCBehaviour for human NPCs.
    // Handles:
    // - Light based visibility checks (objects must be lit to be detected)
    // - Mirror reflection detection (player visible through mirrors triggers game over)
    // - Suspicion reporting to SuspicionManager
    // - Sound delegation to HumanNPCSoundController
    // - Investigation delegation to NPCInvestigationController

    [Header("Suspicion variables")]
    [SerializeField] protected float minSuspiciousRotation;         // Minimum rotation change in degrees to trigger displacement suspicion
    [SerializeField] protected float minSuspiciousPosition;         // Minimum position change to trigger displacement suspicion


    [Header("Mirror Detection")]
    [SerializeField] protected LayerMask mirrorLayer;               // Layer containing mirror objects

    [Header("Lighting")]
    [SerializeField] float detectionRadiusLight = 20f;              // Radius used to find nearby lights when checking if an object is lit
    [SerializeField] LayerMask lightLayer;                          // Layer containing light objects
    [SerializeField] LayerMask wallFloorLayer;                      // Layer containing walls and floors, used to check if light is blocked

    protected HumanNPCSoundController soundController;              // Manages all NPC sound reactions
    protected NPCInvestigationController investigationController;   // Manages investigation queue and coroutines

    protected GameObject player;                                    // Reference to the player GameObject
    private Collider2D playerCollider;                              // Reference to the player collider

    protected bool canSee;                                          // False when the NPC is inside a room and vision is disabled
    protected bool hasSeenPolterg = false;                              // True once the NPC has spotted the player
    protected bool hasSeenMovement = false;                         // True while the NPC has recently seen a moving object


    // Sorting layer IDs
    // Used to control whether the FOV cone light affects possessed objects.
    // This prevents the FOV cone from illuminating objects that are in darkness,
    // which would otherwise reveal them to the player despite being undetectable.
    private string visibleLayer = "Default";
    private string notVisibleLayer = "NotVisible";
    private int visibleLayerID;
    private int notVisibleLayerID;

    protected override void Start()
    {
        base.Start();

        player = GameObject.FindWithTag("Player");
        playerCollider = player.GetComponent<Collider2D>();
        soundController = GetComponent<HumanNPCSoundController>();
        investigationController = GetComponent<NPCInvestigationController>();
        canSee = true;

        // Define the conditions under which the non suspicious sound can play
        soundController.InitializeNonSuspiciousSoundConditions(isNonSuspicious);

        visibleLayerID = SortingLayer.NameToID(visibleLayer);
        notVisibleLayerID = SortingLayer.NameToID(notVisibleLayer);

        // Link investigation events directly to sound controller
        investigationController.OnInvestigationStarted += soundController.OnInvestigationStarted;
        investigationController.OnInvestigationEnded += soundController.OnInvestigationEnded;
    }

    protected override void Update()
    {
        base.Update();

        CheckMirrorReflection();
    }

    // Detection

    // Dispatches detection results to sound and suspicion systems.
    // Differentiates between a new moving object, the same object moved stopped and moved again
    // and no object moving.
    protected override void OnDetectionResult(DetectionResult result)
    {
        if (!result.foundMovingObject)
        {
            soundController.OnObjectMovingStopped();
        }
        else if (!result.wasAlreadyMoving)
        {
            soundController.OnObjectMovingStarted(result.movingObject);
        }
        else
        {
            soundController.OnSameObjectStillMoving(result.movingObject);
        }

        HandleMovementSuspicion(result.objectSize);
        //HandleChangedPositionSuspicion(result.movingObject.GetComponent<PossessionController>(),result.objectSize);
    }

    // Overrides base FOV check with a more precise point based check.
    protected override bool IsObjectInFieldOfView(Collider2D obj)
    {
        if (canSee)
        {
            // Sample multiple points on the object collider for more accurate detection
            Vector2[] colliderPoints = LightUtility.GetSamplePointsFromObject(obj);
            return IsPointInFieldOfView(colliderPoints, obj);
        }
        return false;
    }

    // Checks whether any of the given sample points are:
    // 1. Within the FOV cone angle
    // 2. Within detection radius
    // 3. Illuminated by a nearby light source (unless it's the player)
    // Also updates the object's sorting layer to reflect visibility.
    protected bool IsPointInFieldOfView(Vector2[] colliderPoints, Collider2D objectCollider)
    {
        foreach (Vector2 point in colliderPoints)
        {
            // Check if the object point is in the line of sight of the NPC
            Vector2 directionToPoint = (point - (Vector2)transform.position).normalized;
            float angle = Vector2.Angle(facingRight ? Vector2.right : Vector2.left, directionToPoint);

            // Skip this point if it's outside the field of view angle
            if (angle > fieldOfViewAngle / 2)
            {
                continue;
            }

            // Skip if the point is outside the detection radius
            if (Vector2.Distance(point, transform.position) > detectionRadius)
            {
                continue;
            }

            SpriteRenderer objectSprite = objectCollider.GetComponentInChildren<SpriteRenderer>();
            bool isPlayer = objectCollider.GetComponent<PlayerController>() != null;

            // Non-player objects must be lit to be detected            
            if (!IsObjectLit(objectCollider) && !isPlayer)
            {
                if (objectSprite != null)
                {
                    // The object is in darkness so we exclude its layer from FOV light so it doesn't visually
                    // change color under the cone, signaling to the player it cannot be detected
                    objectSprite.sortingLayerID = notVisibleLayerID;
                }
                continue;
            }

            // Object is visible — update sorting layer for rendering
            if (objectSprite != null && !isPlayer)
            {
                // Object is sufficiently lit so we include its layer in the FOV light rendering
                // so the cone visually highlights it as detectable
                objectSprite.sortingLayerID = visibleLayerID;
            }
            return true;
        }
        return false;
    }

    // Returns true if any nearby light source illuminates the given collider.
    // Uses LightUtility to verify that the light ray is not blocked by walls or floors.
    protected bool IsObjectLit(Collider2D objCollider)
    {
        Collider2D[] lights = Physics2D.OverlapCircleAll(objCollider.bounds.center, detectionRadiusLight, lightLayer);
        foreach (Collider2D lightCollider in lights)
        {
            if (LightUtility.IsPointHitByLight(lightCollider, objCollider, wallFloorLayer))
            {
                return true;
            }
        }
        return false;
    }

    // Scans nearby mirrors each frame.
    // If the player's reflection is visible in a mirror and not blocked, triggers NPCSeePolterg().
    protected void CheckMirrorReflection()
    {
        Collider2D[] mirrors = Physics2D.OverlapCircleAll(transform.position, detectionRadius, mirrorLayer);
        foreach (Collider2D mirrorCollider in mirrors)
        {
            Mirror mirror = mirrorCollider.GetComponentInParent<Mirror>();//GetComponent<Mirror>();
            if (mirror == null) continue;

            // Check if the mirror is in the NPC's FOV
            if (!IsObjectInFieldOfView(mirrorCollider)) continue;

            // Check if the player is reflected in this mirror           
            if (mirror.IsReflectedInMirror(playerCollider))
            {
                Vector2[] reflectionPoints = mirror.GetReflectionPoints(playerCollider);

                if (IsPointInFieldOfView(reflectionPoints, playerCollider))
                {
                    // Check if the reflection is within the NPC's FOV and not obscured
                    if (!mirror.IsMirrorReflectionBlocked(reflectionPoints, playerCollider) && !hasSeenPolterg)
                    {                        
                        NPCSeePolterg();
                    }
                }

            }
        }
    }

    // Suspicion

    // Updates suspicion based on whether a moving object is currently observed.
    // Registers and unregisters the NPC as a paranormal observer in SuspicionManager.
    // Also resets hasSeenMovement when suspicion drops back to zero.
    protected void HandleMovementSuspicion(float objectSize)
    {
        // If the NPC sees an object moving for the first time
        if (isObjectMoving && !isCurrentlyObserving)
        {
            isCurrentlyObserving = true;
            hasSeenMovement = true;
            SuspicionManager.Instance.AddParanormalObserver();
        }
        // If the object has stopped moving
        else if (!isObjectMoving && isCurrentlyObserving)
        {
            isCurrentlyObserving = false;
            SuspicionManager.Instance.RemoveParanormalObserver();
        }
        // If the object is still moving
        if (isObjectMoving && isCurrentlyObserving)
        {
            SuspicionManager.Instance.UpdateMovementSuspicion(objectSize);
        }

        if (hasSeenMovement && SuspicionManager.Instance.CurrentSuspicion <= 0)
            hasSeenMovement = false;
    }

    // Increases suspicion if a possessed object has moved or rotated significantly
    // since it was last seen by this NPC. Only checked when the object is not moving.
    protected void HandleChangedPositionSuspicion(PossessionController possessedObject, float objectSize)
    {
        if (!isObjectMoving)
        {
            // Check if the object has changed significantly of position and rotation
            float positionChange = Vector2.Distance(possessedObject.LastKnownPosition, possessedObject.transform.position);
            float rotationChange = Quaternion.Angle(possessedObject.LastKnownRotation, possessedObject.transform.rotation);

            // The object moved or rotated too much out of sight of the NPC
            if (positionChange >= minSuspiciousPosition || rotationChange >= minSuspiciousRotation)
            {
                SuspicionManager.Instance.UpdateDisplacementSuspicion(objectSize, rotationChange, positionChange);
            }
        }
        // Update the new position and rotation of the object
        possessedObject.UpdateLastKnownPositionRotation();
    }

    // Called when the NPC sees the player through a mirror.
    // Triggers max suspicion and reset to the last checkpoint.
    protected void NPCSeePolterg()
    {
        hasSeenPolterg = true;
        soundController.OnPoltergSeen();
        SuspicionManager.Instance.UpdateSeeingPoltergSuspicion();
        player.GetComponent<MovementController>().PlayerGotCaught();
    }


    // Investigation


    // Enqueues a sound triggered investigation at the given object's position.
    // If replaceObject is true, the object will be reset to its original position after investigation.
    // The target floor is the floor where the sound come from.
    public virtual void InvestigateSound(SoundDetection objectsound, bool replaceObject, float targetFloor)
    {
        investigationController.EnqueueSoundInvestigation(objectsound, replaceObject, targetFloor);
    }

    // Enqueues a custom investigation coroutine directly.
    // Used for scripted investigations triggered by external systems.
    // Used in PowerOutage when the electricity is cut off.
    public void EnqueueInvestigation(IEnumerator investigation)
    {
        investigationController.EnqueueInvestigation(investigation);
    }

    // Return the NPC to it's initial position and facing direction after the end of an investigation
    public virtual IEnumerator ReturnToInitialPosition()
    {
        yield return npcMovementController.ReachTarget(
            initialPosition,
            currentFloorLevel,
            initialFloorLevel);
        SetFacingDirection(initialFacingRight);
    }


    // Sound

    // Return whether everything is normal for the NPC to sound non suspicious based on current game state.
    private bool isNonSuspicious()
    {
        return !isObjectMoving &&
               !investigationController.IsInvestigating &&
               investigationController.QueueCount == 0 &&
               !hasSeenPolterg;
    }

    // Display Icon

    // Returns the icon state to display above the NPC based on current game state.
    protected override IconState GetIconState()
    {
        if (hasSeenPolterg) return IconState.Alert;
        if (!investigationController.HasActiveInvestigation && SuspicionManager.Instance.HasSuspicionDecrease) return IconState.None;
        if (hasSeenMovement && SuspicionManager.Instance.CurrentSuspicion > 0) return IconState.Alert;
        if (investigationController.HasActiveInvestigation || investigationController.QueueCount > 0) return IconState.Investigation;

        return IconState.None;
    }

    // Reset
    public override void ResetInitialState()
    {
        base.ResetInitialState();
        StopAllCoroutines();

        investigationController.ResetState();
        soundController.Reset();

        canSee = true;
        hasSeenPolterg = false;
        hasSeenMovement = false;       
        npcMovementController.Reset();
    }

    // Resets only the polterg detection flag. Used when restarting from checkpoint
    // without doing a full state reset.
    public void ResetSeePolterg()
    {
        hasSeenPolterg = false;
    }

    // Unsubscribe to prevent callbacks on destroyed objects
    private void OnDisable()
    {
        investigationController.OnInvestigationStarted -= soundController.OnInvestigationStarted;
        investigationController.OnInvestigationEnded -= soundController.OnInvestigationEnded;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadiusLight);

        Gizmos.color = UnityEngine.Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
