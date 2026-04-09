using System.Collections;
using UnityEngine;

// Utilisé exclusivement par PatrollingNPCBehaviour
// Override RunInvestigation pour attendre que le NPC soit dans un état valide
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
        // Attend que le NPC soit disponible avant de commencer
        while (patrollingBehaviour.IsBlocked || patrollingBehaviour.IsInRoom || patrollingBehaviour.IsWaiting)
            yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(base.RunInvestigation(investigation));
    }
}