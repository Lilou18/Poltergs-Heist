using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NPCMovementController))]
[RequireComponent(typeof(BasicNPCBehaviour))]
public class NPCInvestigationController : MonoBehaviour
{
    [Header("Investigation variables")]
    [SerializeField] protected float surpriseWaitTime = 2f;
    [SerializeField] protected float investigationWaitTime = 3f;

    public event Action OnInvestigationStarted;
    public event Action OnInvestigationEnded;
    public event Action OnAllInvestigationsCleared;

    public bool IsInvestigating => isInvestigating;
    public bool HasActiveInvestigation => hasActiveInvestigation;
    public int QueueCount => investigationQueue.Count;

    protected bool isInvestigating = false;
    protected bool hasActiveInvestigation = false;
    private bool isAtInitialPosition = true;

    private Queue<IEnumerator> investigationQueue = new Queue<IEnumerator>();
    private Coroutine currentInvestigation = null;
    private Coroutine returnToInitialPositionCoroutine = null;

    protected NPCMovementController npcMovementController;
    protected BasicNPCBehaviour npcBehaviour;

    protected virtual void Awake()
    {
        npcMovementController = GetComponent<NPCMovementController>();
        npcBehaviour = GetComponent<BasicNPCBehaviour>();
    }

    private void Update()
    {
        if (investigationQueue.Count > 0 && !isInvestigating)
        {
            if (returnToInitialPositionCoroutine != null)
            {
                StopCoroutine(returnToInitialPositionCoroutine);
                returnToInitialPositionCoroutine = null;
            }

            isAtInitialPosition = false;
            hasActiveInvestigation = true;
            currentInvestigation = StartCoroutine(RunInvestigation(investigationQueue.Dequeue()));
            OnInvestigationStarted?.Invoke();
        }
        else if (investigationQueue.Count == 0 && !isInvestigating && !isAtInitialPosition)
        {
            isAtInitialPosition = true;
            returnToInitialPositionCoroutine = StartCoroutine(ReturnToInitialPosition());
        }
    }

    public void EnqueueInvestigation(IEnumerator investigation)
    {
        hasActiveInvestigation = true;
        investigationQueue.Enqueue(investigation);
    }

    public void EnqueueSoundInvestigation(SoundDetection objectSound, bool replaceObject, float targetFloor)
    {
        hasActiveInvestigation = true;
        investigationQueue.Enqueue(InvestigateSoundObject(objectSound, replaceObject, targetFloor));
    }

    protected virtual IEnumerator RunInvestigation(IEnumerator investigation)
    {
        isInvestigating = true;
        yield return StartCoroutine(investigation);
        isInvestigating = false;
        currentInvestigation = null;

        if (investigationQueue.Count == 0)
        {
            hasActiveInvestigation = false;
            OnAllInvestigationsCleared?.Invoke();
        }

        OnInvestigationEnded?.Invoke();
    }

    private IEnumerator InvestigateSoundObject(SoundDetection objectSound, bool replaceObject, float targetFloor)
    {
        // Take a surprise pause before going on investigation
        npcMovementController.Reset();
        yield return new WaitForSeconds(surpriseWaitTime);

        yield return npcMovementController.ReachTarget(objectSound.transform.position, npcBehaviour.FloorLevel, targetFloor);

        // We can't find a path
        if (!npcMovementController.CanFindPath) yield break;

        // Wait a bit of time before going back to normal
        yield return new WaitForSeconds(investigationWaitTime);

        IResetObject resetObject = objectSound.GetComponent<IResetObject>();
        if (resetObject != null && replaceObject)
            resetObject.ResetObject();
    }

    // Return the NPC to it's initial position and facing direction
    private IEnumerator ReturnToInitialPosition()
    {
        yield return StartCoroutine(npcMovementController.ReachTarget(
            npcBehaviour.InitialPosition,
            npcBehaviour.FloorLevel,
            npcBehaviour.InitialFloorLevel));

        // Restore initial facing direction
        if (npcBehaviour.FacingRight != npcBehaviour.IniFacingRight)
        {
            npcBehaviour.FacingRight = npcBehaviour.IniFacingRight;
            npcBehaviour.FlipFieldOfView();
        }
    }

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
