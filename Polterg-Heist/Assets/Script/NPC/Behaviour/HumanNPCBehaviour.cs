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
    protected bool canSee;  // Ability of the player to see

    [Header("Mirror")]
    [SerializeField] protected LayerMask mirrorLayer;   // Layer of the mirrors

    protected GameObject player;

    [Header("NPC sound variables")]
    [SerializeField] protected AK.Wwise.Event curiousNPCSoundEvent;
    [SerializeField] protected AK.Wwise.Event nonSuspiciousSoundEvent;
    [SerializeField] protected float soundCooldown = 1.5f;  // Cooldown between sound of surprise
    protected float lastSoundTime;
    protected GameObject lastMovingObject;
    protected bool soundHasPlayed;

    protected float nonSuspiciousSoundCooldown = 0f;//3f;  // Cooldown before restarting ambient sound
    protected float lastSuspiciousTime;  // Last time something suspicious happened
    protected bool isNonSuspiciousSoundPlaying = false;  // Is the ambient sound currently playing
    protected Coroutine nonSuspiciousSoundCoroutine = null;


    [Header("Investigation Variables")]
    [SerializeField] protected Sprite investigationIcon;
    protected NPCInvestigationController investigationController;
    public AudioSource audioSource;  // Source of the surprised sound


    [Header("Lighting Variable")]
    [SerializeField] float detectionRadiusLight = 20f;
    [SerializeField] LayerMask lightLayer;  // Layer of the gameobject light
    [SerializeField] LayerMask wallFloorLayer;   // Layer of the gameobject wall
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
        audioSource = GetComponent<AudioSource>();        
        canSee = true;

        // Initialize sound tracking variables
        lastMovingObject = null;
        soundHasPlayed = false;
        lastSoundTime = -soundCooldown;
        lastSuspiciousTime = -nonSuspiciousSoundCooldown;

        visibleLayerID = SortingLayer.NameToID(visibleLayer);
        notVisibleLayerID = SortingLayer.NameToID(notVisibleLayer);

        investigationController = GetComponent<NPCInvestigationController>();

        investigationController.OnInvestigationStarted += HandleInvestigationStarted;
        investigationController.OnInvestigationEnded += HandleInvestigationEnded;
    }

    //private Coroutine returnToInitialPositionCoroutine;
    protected override void Update()
    {
        base.Update();

        UpdateIconDisplay();

        CheckMirrorReflection();

        // If investigation started or we see the poltergeist, stop non suspicious sound
        if ((investigationController.IsInvestigating || investigationController.QueueCount > 0 || seePolterg) && isNonSuspiciousSoundPlaying)
        {
            StopNonSuspiciousSound();
        }
    }

    protected override void OnDetectionResult(DetectionResult result)
    {
        if (!result.foundMovingObject && lastMovingObject != null)
            soundHasPlayed = false;

        if (result.foundMovingObject && !result.wasAlreadyMoving)
            StopNonSuspiciousSound();

        if (result.foundMovingObject)
            HandleSoundEvent(result.movingObject);

        HandleMovementSuspicion(result.objectSize);
        //HandleChangedPositionSuspicion(result.movingObject.GetComponent<PossessionController>(),result.objectSize);
    }

    // Investigation callback
    private void HandleInvestigationStarted()
    {
        StopNonSuspiciousSound();
        curiousNPCSoundEvent.Post(gameObject);
    }

    private void HandleInvestigationEnded()
    {
        lastSuspiciousTime = Time.time;

        if (CanPlayNonSuspiciousSound() && !isNonSuspiciousSoundPlaying)
            StartNonSuspiciousSound();
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
            if (alertSpriteRenderer != null)
            {
                alertSpriteRenderer.sprite = alertIcon;
                fovLight.color = alertColorFOV;
                alertSpriteRenderer.enabled = true;
            }
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
    }

    protected virtual void UpdateIconDisplay()
    {
        if (alertSpriteRenderer == null) return;

        // We don't show anything
        if (!investigationController.HasActiveInvestigation && SuspicionManager.Instance.HasSuspicionDecrease)
        {
            alertSpriteRenderer.enabled = false;
            fovLight.color = nonSuspiciousColorFOV;
        }
        // There is no investigation and there is no possessed object moving in front of the NPC
        else if (!investigationController.HasActiveInvestigation && investigationController.QueueCount == 0 && !hasSeenMovement)
        {
            alertSpriteRenderer.enabled = false;
            fovLight.color = nonSuspiciousColorFOV;
        }
        // Case 2: If there is an investigation and nothing to alert
        else if (investigationController.HasActiveInvestigation && (!hasSeenMovement || SuspicionManager.Instance.HasSuspicionDecrease))
        {
            alertSpriteRenderer.sprite = investigationIcon;
            alertSpriteRenderer.enabled = true;
            fovLight.color = nonSuspiciousColorFOV;
        }
        // Case 1: The NPC saw an object moving
        else if (hasSeenMovement && SuspicionManager.Instance.CurrentSuspicion > 0)
        {
            alertSpriteRenderer.sprite = alertIcon;
            fovLight.color = alertColorFOV;
            alertSpriteRenderer.enabled = true;
        }

        if (hasSeenMovement && SuspicionManager.Instance.CurrentSuspicion <= 0)
        {
            hasSeenMovement = false;

            if (investigationController.HasActiveInvestigation)
            {
                alertSpriteRenderer.sprite = investigationIcon;
                fovLight.color = nonSuspiciousColorFOV;
                alertSpriteRenderer.enabled = true;
            }
        }
    }

    // Manage the sound made by the NPC when he sees an object moving
    protected virtual void HandleSoundEvent(GameObject currentMovingObject)
    {
        // Check if we are past the cooldown
        bool cooldownElapsed = (Time.time - lastSoundTime) >= soundCooldown;

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
        else if (lastMovingObject == currentMovingObject && !soundHasPlayed && cooldownElapsed)
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
        if (nonSuspiciousSoundCoroutine != null)
        {
            return;
        }
        nonSuspiciousSoundCoroutine = StartCoroutine(PlayNonSuspiciousSound());
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
            if (nonSuspiciousSoundEvent != null)
            {
                nonSuspiciousSoundEvent.Post(gameObject);
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
        bool baseConditions = !isObjectMoving;

        bool notInvestigating = !investigationController.IsInvestigating && investigationController.QueueCount == 0;
        bool notSeeingReflection = !seePolterg;

        return baseConditions && notInvestigating && notSeeingReflection;
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
                        if (isNonSuspiciousSoundPlaying)
                        {
                            StopNonSuspiciousSound();
                        }
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
        audioSource.Play();
        surpriseSoundEvent.Post(gameObject);
        npcAnimMouth.SetTrigger("IsSurprised");

        if(alertSpriteRenderer != null)
        {
            alertSpriteRenderer.sprite = alertIcon;
            fovLight.color = alertColorFOV;
            alertSpriteRenderer.enabled = true;
        }

        SuspicionManager.Instance.UpdateSeeingPoltergSuspicion();
    }

    public override void ResetInitialState()
    {
        base.ResetInitialState();
        StopAllCoroutines();
        canSee = true;
        seePolterg = false;


        hasSeenMovement = false;

        if(alertSpriteRenderer != null)
        {
            alertSpriteRenderer.enabled = false;
        }
        fovLight.color = nonSuspiciousColorFOV;
        // Reset sounds
        if (nonSuspiciousSoundEvent != null)
        {
            nonSuspiciousSoundEvent.Stop(gameObject);
        }

        lastMovingObject = null;
        soundHasPlayed = false;
        StopNonSuspiciousSound();
        lastSuspiciousTime = -nonSuspiciousSoundCooldown;
        isNonSuspiciousSoundPlaying = false;

        npcAnim.SetBool("InMovement", false);
        npcAnimMouth.SetBool("IsSurprised", false);
        npcMovementController.Reset();
        fovLight.color = nonSuspiciousColorFOV;
    }

    public void ResetSeePolterg()
    {
        seePolterg = false;
    }

    private void OnDisable()
    {
        investigationController.OnInvestigationStarted -= HandleInvestigationStarted;
        investigationController.OnInvestigationEnded -= HandleInvestigationEnded;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadiusLight);

        Gizmos.color = UnityEngine.Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
