using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NPCMovementController))]
[RequireComponent(typeof(HumanNPCBehaviour))]
public class NPCInvestigationController : MonoBehaviour
{
    // Manages the NPC investigation queue.
    // Investigations are triggered by sounds or external scripts and processed one at a time.
    // Once all investigations are done, the NPC returns to its initial position.


    [Header("Investigation variables")]
    [SerializeField] protected float surpriseWaitTime = 2f;                     // Duration the NPC pauses in surprise before moving to investigate
    [SerializeField] protected float investigationWaitTime = 3f;                // Duration the NPC waits at the investigation target before returning

    protected bool isInvestigating = false;                                     // True while RunInvestigation is running
    protected bool hasActiveInvestigation = false;                              // True until the last investigation ends
    private bool isAtInitialPosition = true;                                    // False while the NPC is away from its initial position

    private Queue<IEnumerator> investigationQueue = new Queue<IEnumerator>();   // Pending investigations to process
    private Coroutine currentInvestigation = null;                              // Currently running investigation coroutine
    private Coroutine returnToInitialPositionCoroutine = null;                  // Coroutine returning NPC to start position

    protected NPCMovementController npcMovementController;                      // Used to move the NPC during investigations
    protected HumanNPCBehaviour npcBehaviour;                                   // Used to read initial position and floor level

    public event Action OnInvestigationStarted;                                 // Fired when a new investigation begins
    public event Action OnInvestigationEnded;                                   // Fired when the current investigation ends

    // Getters
    public bool IsInvestigating => isInvestigating;
    public bool HasActiveInvestigation => hasActiveInvestigation;
    public int QueueCount => investigationQueue.Count;

    protected virtual void Awake()
    {
        npcMovementController = GetComponent<NPCMovementController>();
        npcBehaviour = GetComponent<HumanNPCBehaviour>();
    }

    // Processes the investigation queue each frame.
    // Starts the next investigation when the previous one ends.
    // Triggers return to initial position when the queue is empty.
    private void Update()
    {
        if (investigationQueue.Count > 0 && !isInvestigating)
        {
            // A new investigation is ready so we cancel any return to initial position coroutine
            if (returnToInitialPositionCoroutine != null)
            {
                StopCoroutine(returnToInitialPositionCoroutine);
                returnToInitialPositionCoroutine = null;
            }

            // Start the investigation and remove it from the queue
            isAtInitialPosition = false;
            hasActiveInvestigation = true;            
            currentInvestigation = StartCoroutine(RunInvestigation(investigationQueue.Dequeue()));
            OnInvestigationStarted?.Invoke();
        }
        // Investigation queue is empty and the NPC is not at its start position, so we make him
        // go back to initial position
        else if (investigationQueue.Count == 0 && !isInvestigating && !isAtInitialPosition)
        {
            isAtInitialPosition = true;
            returnToInitialPositionCoroutine = StartCoroutine(ReturnToInitialPosition());
        }
    }

    // Enqueues a custom investigation coroutine.
    // Used for scripted investigations triggered by external systems.
    public void EnqueueInvestigation(IEnumerator investigation)
    {
        hasActiveInvestigation = true;
        investigationQueue.Enqueue(investigation);
    }

    // Enqueues a sound-triggered investigation at the given object's position.
    // If replaceObject is true, the object is reset to its original position after the investigation.
    // targetFloor is the floor level where the sound originated.
    public void EnqueueSoundInvestigation(SoundDetection objectSound, bool replaceObject, float targetFloor)
    {
        hasActiveInvestigation = true;
        investigationQueue.Enqueue(InvestigateSoundObject(objectSound, replaceObject, targetFloor));
    }

    // Wraps an investigation coroutine and manages isInvestigating state.
    // Fires OnInvestigationEnded when the investigation finishes.
    protected virtual IEnumerator RunInvestigation(IEnumerator investigation)
    {
        // Investigation started
        isInvestigating = true;
        yield return StartCoroutine(investigation);

        // Investigation ended
        isInvestigating = false;
        currentInvestigation = null;

        if (investigationQueue.Count == 0)
        {
            hasActiveInvestigation = false;
        }

        OnInvestigationEnded?.Invoke();
    }

    // Moves the NPC to the sound source, waits, then optionally resets the object.
    private IEnumerator InvestigateSoundObject(SoundDetection objectSound, bool replaceObject, float targetFloor)
    {
        // Step 1: Take a surprise pause before going on investigation
        npcMovementController.Reset();
        yield return new WaitForSeconds(surpriseWaitTime);

        // Step 2: Navigate to the sound source
        yield return npcMovementController.ReachTarget(objectSound.transform.position, npcBehaviour.FloorLevel, targetFloor);

        // Abort if no valid path was found
        if (!npcMovementController.CanFindPath) yield break;

        // Step 3: Wait at the location to investigate
        yield return new WaitForSeconds(investigationWaitTime);

        // Step 4: Reset the object if requested
        IResetObject resetObject = objectSound.GetComponent<IResetObject>();
        if (resetObject != null && replaceObject)
            resetObject.ResetObject();
    }

    // Return the NPC to it's initial position and facing direction
    protected virtual IEnumerator ReturnToInitialPosition()
    {
        yield return StartCoroutine(npcBehaviour.ReturnToInitialPosition());
    }

    // Resets the investigation state: clears the queue and stops all coroutines.
    // Called on respawn or level reset.
    public virtual void ResetState()
    {
        StopAllCoroutines();
        investigationQueue.Clear();
        isInvestigating = false;
        hasActiveInvestigation = false;
        isAtInitialPosition = true;
        currentInvestigation = null;
        returnToInitialPositionCoroutine = null;
    }
}
