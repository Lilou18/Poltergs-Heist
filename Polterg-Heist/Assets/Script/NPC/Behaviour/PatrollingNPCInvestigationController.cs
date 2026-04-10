using System.Collections;
using UnityEngine;

public class PatrollingNPCInvestigationController : NPCInvestigationController
{
    private PatrollingNPCBehaviour patrollingBehaviour;

    protected override void Awake()
    {
        base.Awake();
        patrollingBehaviour = GetComponent<PatrollingNPCBehaviour>();
    }

    protected override IEnumerator RunInvestigation(IEnumerator investigation)
    {
        isInvestigating = true;
        while (patrollingBehaviour.CurrentState == PatrollingNPCState.InRoom || patrollingBehaviour.CurrentState == PatrollingNPCState.Blocked)
            yield return new WaitForSeconds(0.5f);

        patrollingBehaviour.CurrentState = PatrollingNPCState.Investigating;
        patrollingBehaviour.StopPatrolling();

        while (patrollingBehaviour.CurrentState == PatrollingNPCState.WaitingAtPoint)
            yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(base.RunInvestigation(investigation));
    }

    protected override IEnumerator ReturnToInitialPosition()
    {
        patrollingBehaviour.CurrentState = PatrollingNPCState.Returning;

        yield return base.ReturnToInitialPosition();

        patrollingBehaviour.CurrentState = PatrollingNPCState.Idle;
    }
}