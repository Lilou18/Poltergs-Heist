using System;
using System.Collections;
using UnityEngine;

public class HumanNPCSoundController : MonoBehaviour
{
    [Header("Sound Events")]
    [SerializeField] private AK.Wwise.Event surpriseSoundEvent;
    [SerializeField] private AK.Wwise.Event curiousSoundEvent;
    [SerializeField] private AK.Wwise.Event nonSuspiciousSoundEvent;

    [SerializeField] private float surpriseCooldown = 1.5f;

    private Animator npcAnimMouth;
    private GameObject lastMovingObject;
    private Func<bool> canPlayNonSuspiciousSound;
    private bool soundHasPlayed;
    private float lastSoundTime;

    private void Awake()
    {
        Animator npcAnim = GetComponentInChildren<Animator>();
        npcAnimMouth = npcAnim.transform.GetChild(0).GetComponentInChildren<Animator>();
    }

    public void InitializeNonSuspiciousSoundConditions(Func<bool> conditions)
    {
        canPlayNonSuspiciousSound = conditions;
    }

    public void AddNonSuspiciousSoundConditions(Func<bool> newConditions)
    {
        Func<bool> existingConditions = canPlayNonSuspiciousSound;
        canPlayNonSuspiciousSound = () => (existingConditions == null || existingConditions()) && newConditions();
    }

    public void OnObjectMovingStarted(GameObject movingObject)
    {
        StopNonSuspiciousSound();
        HandleSurpriseSound(movingObject);
    }

    public void OnObjectMovingStopped()
    {
        soundHasPlayed = false;
    }

    public void OnSameObjectStillMoving(GameObject movingObject)
    {
        //StopNonSuspiciousSound();
        HandleSurpriseSound(movingObject);
    }

    public void OnInvestigationStarted()
    {
        StopNonSuspiciousSound();
        curiousSoundEvent.Post(gameObject);
    }

    public void OnInvestigationEnded()
    {
        TryPlayNonSuspiciousSound();
    }

    public void TryPlayNonSuspiciousSound()
    {
        if (canPlayNonSuspiciousSound != null && !canPlayNonSuspiciousSound()) return;
        if (nonSuspiciousSoundEvent != null)
            nonSuspiciousSoundEvent.Post(gameObject);
    }

    public void OnPoltergSeen()
    {
        StopNonSuspiciousSound();
        surpriseSoundEvent.Post(gameObject);
        npcAnimMouth.SetTrigger("IsSurprised");
    }

    private void HandleSurpriseSound(GameObject movingObject)
    {
        bool cooldownElapsed = (Time.time - lastSoundTime) >= surpriseCooldown;
        bool isNewObject = lastMovingObject != movingObject;
        bool canRepeat = !soundHasPlayed && cooldownElapsed;

        if (!isNewObject && !canRepeat) return;

        surpriseSoundEvent.Post(gameObject);
        npcAnimMouth.SetTrigger("IsSurprised");
        lastMovingObject = movingObject;
        soundHasPlayed = true;
        lastSoundTime = Time.time;
    }

    private void StopNonSuspiciousSound()
    {
        if (nonSuspiciousSoundEvent != null)
            nonSuspiciousSoundEvent.Stop(gameObject);
    }

    public void Reset()
    {
        StopAllCoroutines();
        npcAnimMouth.SetBool("IsSurprised", false);
        lastMovingObject = null;
        soundHasPlayed = false;
        lastSoundTime = -surpriseCooldown;

        if (nonSuspiciousSoundEvent != null)
            nonSuspiciousSoundEvent.Stop(gameObject);
    }

}
