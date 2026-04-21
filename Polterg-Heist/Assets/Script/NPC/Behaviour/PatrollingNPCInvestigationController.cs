using System.Collections;
using UnityEngine;

public class PatrollingNPCInvestigationController : NPCInvestigationController
{
    // Extends NPCInvestigationController for patrolling NPCs.
    // Adds state management around investigations:
    // - Waits for the NPC to finish room entry/exit before investigating
    // - Stops patrolling before starting an investigation
    // - Manages Returning and Idle states around ReturnToInitialPosition

    private PatrollingNPCBehaviour patrollingBehaviour; // Used to read and set patrol state during investigations

    protected override void Awake()
    {
        base.Awake();
        patrollingBehaviour = GetComponent<PatrollingNPCBehaviour>();
    }

    // Extends NPCInvestigationController with patrol specific state handling.
    // Ensures proper transitions between patrol and investigation states.
    protected override IEnumerator RunInvestigation(IEnumerator investigation)
    {
        // Set immediately so Update() doesn't dequeue another investigation
        // while waiting for the NPC to finish its current room/blocked state
        isInvestigating = true;

        // Wait until the NPC is no longer inside a room or blocked at a door
        while (patrollingBehaviour.CurrentState == PatrollingNPCState.InRoom || patrollingBehaviour.CurrentState == PatrollingNPCState.Blocked)
            yield return new WaitForSeconds(0.5f);

        // Stops patrolling and sets state to Investigating
        patrollingBehaviour.CurrentState = PatrollingNPCState.Investigating;
        patrollingBehaviour.StopPatrolling();

        // Wait for any in progress waiting animation to complete before moving
        while (patrollingBehaviour.CurrentState == PatrollingNPCState.WaitingAtPoint)
            yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(base.RunInvestigation(investigation));
    }

    // Overrides return behavior to integrate the PatrollingNPCState.
    protected override IEnumerator ReturnToInitialPosition()
    {
        patrollingBehaviour.CurrentState = PatrollingNPCState.Returning;

        yield return base.ReturnToInitialPosition();

        patrollingBehaviour.CurrentState = PatrollingNPCState.Idle;
    }
}