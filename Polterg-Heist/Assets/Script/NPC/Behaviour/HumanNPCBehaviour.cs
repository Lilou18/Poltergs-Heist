using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class HumanNPCBehaviour : BasicNPCBehaviour
{
    // Sound variables
    

    [Header("Suspicion variables")]
    // Variable manage suspicion of the NPC
    [SerializeField] protected float minSuspiciousRotation; // Minimum rotation change in degrees to trigger suspicion
    [SerializeField] protected float minSuspiciousPosition; // Minimum position change to trigger suspicion
    

    [Header("Mirror")]
    [SerializeField] protected LayerMask mirrorLayer;   // Layer of the mirrors

    protected GameObject player;

    [Header("NPC sound variables")]
    protected HumanNPCSoundController soundController;
    
    [Header("Investigation Variables")]
    protected NPCInvestigationController investigationController;

    [Header("Lighting Variable")]
    [SerializeField] float detectionRadiusLight = 20f;
    [SerializeField] LayerMask lightLayer;  // Layer of the gameobject light
    [SerializeField] LayerMask wallFloorLayer;   // Layer of the gameobject wall
    protected bool canSee;  // Ability of the player to see
    string visibleLayer = "Default";
    string notVisibleLayer = "NotVisible";
    int visibleLayerID;
    int notVisibleLayerID;


    protected bool seePolterg = false;
    [SerializeField] protected bool hasSeenMovement = false;

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindWithTag("Player");         
        soundController = GetComponent<HumanNPCSoundController>();
        investigationController = GetComponent<NPCInvestigationController>();
        canSee = true;

        // Initialize sound tracking variables
        soundController.InitializeNonSuspiciousSoundConditions(() => 
        !isObjectMoving &&
        !investigationController.IsInvestigating &&
        investigationController.QueueCount == 0 &&
        !seePolterg);

        visibleLayerID = SortingLayer.NameToID(visibleLayer);
        notVisibleLayerID = SortingLayer.NameToID(notVisibleLayer);



        
        investigationController.OnInvestigationStarted += soundController.OnInvestigationStarted;
        investigationController.OnInvestigationEnded += soundController.OnInvestigationEnded;
    }

    //private Coroutine returnToInitialPositionCoroutine;
    protected override void Update()
    {
        base.Update();

        CheckMirrorReflection();
    }

    protected override IconState GetIconState()
    {
        if (seePolterg) return IconState.Alert;
        if (!investigationController.HasActiveInvestigation && SuspicionManager.Instance.HasSuspicionDecrease) return IconState.None;
        if (hasSeenMovement && SuspicionManager.Instance.CurrentSuspicion > 0) return IconState.Alert;
        if (investigationController.HasActiveInvestigation || investigationController.QueueCount > 0) return IconState.Investigation;
             
        return IconState.None;
    }

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
    public virtual void InvestigateSound(SoundDetection objectsound, bool replaceObject, float targetFloor)
    {
        investigationController.EnqueueSoundInvestigation(objectsound, replaceObject, targetFloor);
    }

    public void EnqueueInvestigation(IEnumerator investigation)
    {
        investigationController.EnqueueInvestigation(investigation);
    }

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
                //possessedObject.UpdateLastKnownPositionRotation();
                SuspicionManager.Instance.UpdateDisplacementSuspicion(objectSize, rotationChange, positionChange);
            }
        }
        // Update the new position and rotation of the object
        possessedObject.UpdateLastKnownPositionRotation();
    }

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

    // Verify if the object is in the field of view of the NPC
    protected override bool IsObjectInFieldOfView(Collider2D obj)
    {
        if (canSee)
        {
            // Check if any part of the object is seen 
            Vector2[] colliderPoints = LightUtility.GetSamplePointsFromObject(obj);

            return IsPointInFieldOfView(colliderPoints, obj);
        }
        return false;
    }

    // Verifiy if the object is toutched by a light
    protected bool IsObjectLit(Collider2D objCollider)
    {
        Collider2D[] lights = Physics2D.OverlapCircleAll(objCollider.bounds.center, detectionRadiusLight, lightLayer);
        foreach(Collider2D lightCollider in lights)
        {
            if(LightUtility.IsPointHitByLight(lightCollider, objCollider, wallFloorLayer))
            {
                return true;
            }
        }
        return false;
    }

    // Verifiy if any parts of the object is in the field of view
    protected bool IsPointInFieldOfView(Vector2[] colliderPoints, Collider2D objectCollider)
    {
        foreach (Vector2 point in colliderPoints)
        {
            // Check if the object is in the line of sight of the NPC
            Vector2 directionToPoint = (point - (Vector2)transform.position).normalized;
            float angle = Vector2.Angle(facingRight ? Vector2.right : Vector2.left, directionToPoint);
            
            // If the object is not within view angle, return false immediately
            if (angle > fieldOfViewAngle / 2)
            {
                continue;
            }

            if(Vector2.Distance(point, transform.position) > detectionRadius)
            {
                continue;
            }

            SpriteRenderer objectSprite = objectCollider.GetComponentInChildren<SpriteRenderer>();
            // Check if there is light toutching the object
            if (!IsObjectLit(objectCollider) && !objectCollider.GetComponent<PlayerController>())
            {
                if(objectSprite != null)
                {
                    objectSprite.sortingLayerID = notVisibleLayerID;
                }
                continue;
            }

            // Object is in field of view and area is sufficiently lit
            if (objectSprite != null && !objectCollider.GetComponent<PlayerController>())
            {
                int objectSortingLayer = objectSprite.sortingLayerID;
                objectSprite.sortingLayerID = visibleLayerID;
            }
            return true;
        }
        return false;
    }

    // Check if we can see the player trough the mirror
    protected void CheckMirrorReflection()
    {
        Collider2D[] mirrors = Physics2D.OverlapCircleAll(transform.position, detectionRadius, mirrorLayer);
        foreach (Collider2D mirrorCollider in mirrors)
        {
            Mirror mirror = mirrorCollider.GetComponentInParent<Mirror>();//GetComponent<Mirror>();
            if (mirror == null) continue;

            // Check if the mirror is in the field of view of the NPC
            if (!IsObjectInFieldOfView(mirrorCollider)) continue;

            // Check if the player reflection is in the mirror
            if (mirror.IsReflectedInMirror(player.GetComponent<Collider2D>()))
            {
                Collider2D playerCollider = player.GetComponent<Collider2D>();
                Vector2[] reflectionPoints = mirror.GetReflectionPoints(playerCollider);

                if (IsPointInFieldOfView(reflectionPoints, playerCollider))
                {
                    // If nothing is blocking the sight of the NPC to the reflection of the player
                    if (!mirror.IsMirrorReflectionBlocked(reflectionPoints, playerCollider) && !seePolterg)
                    {
                        playerCollider.gameObject.GetComponent<MovementController>().canMove = false;
                        NPCSeePolterg();
                    }
                } 

            }
        }
    }

    // When the Npc see Polterg it's gameover
    protected void NPCSeePolterg()
    {
        seePolterg = true;
        soundController.OnPoltergSeen();
        SuspicionManager.Instance.UpdateSeeingPoltergSuspicion();
    }

    public override void ResetInitialState()
    {
        base.ResetInitialState();
        StopAllCoroutines();

        investigationController.ResetState();
        soundController.Reset();

        canSee = true;
        seePolterg = false;
        hasSeenMovement = false;       

        npcAnim.SetBool("InMovement", false);

        npcMovementController.Reset();
    }

    public void ResetSeePolterg()
    {
        seePolterg = false;
    }

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
