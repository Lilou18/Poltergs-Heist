using System;
using UnityEngine;

public class HumanNPCSoundController : MonoBehaviour
{
    // Manages all sound reactions for human NPCs.
    // Handles:
    // - Surprise sound when a moving object is detected
    // - Curious sound when an investigation starts
    // - Non-suspicious ambient sound when the NPC is calm
    // - NPC see Polterg

    [Header("Sound Events")]
    [SerializeField] private AK.Wwise.Event surpriseSoundEvent;         // Sound event played when the NPC notices a moving object or sees Polterg
    [SerializeField] private AK.Wwise.Event curiousSoundEvent;          // Sound event played when the NPC starts investigating
    [SerializeField] private AK.Wwise.Event nonSuspiciousSoundEvent;    // Sound event played when nothing suspicious happen

    [Header("Timings")]
    [SerializeField] private float surpriseCooldown = 1.5f;             // Minimum time between surprise sounds for the same object

    // Condition evaluated before playing the non suspicious sound.
    // Injected via InitializeNonSuspiciousSoundConditions and optionally extended
    // via AddNonSuspiciousSoundConditions.
    private Func<bool> canPlayNonSuspiciousSound;                       

    private Animator npcAnimMouth;                                      // Mouth animator, triggered when the NPC reacts with surprise
    private GameObject lastMovingObject;                                // Last object that triggered a surprise sound
    private bool soundHasPlayed;                                        // True after the surprise sound has played and reset when the object stops moving
    private float lastSoundTime;                                        // Timestamp of the last surprise sound, used to enforce the cooldown

    private void Awake()
    {
        // The mouth animator is a child of the main NPC animator
        Animator npcAnim = GetComponentInChildren<Animator>();
        npcAnimMouth = npcAnim.transform.GetChild(0).GetComponentInChildren<Animator>();
    }

    // Sets the base condition for playing the non suspicious sound.
    public void InitializeNonSuspiciousSoundConditions(Func<bool> conditions)
    {
        canPlayNonSuspiciousSound = conditions;
    }

    // Chains an additional condition onto the existing non suspicious sound condition.
    // Used by PatrollingNPCBehaviour
    public void AddNonSuspiciousSoundConditions(Func<bool> newConditions)
    {
        Func<bool> existingConditions = canPlayNonSuspiciousSound;
        canPlayNonSuspiciousSound = () => (existingConditions == null || existingConditions()) && newConditions();
    }

    // Called when a new moving object is detected for the first time.
    // Stops the ambient sound and plays the surprise reaction.
    public void OnObjectMovingStarted(GameObject movingObject)
    {
        StopNonSuspiciousSound();
        HandleSurpriseSound(movingObject);
    }

    // Called when no moving object is detected this frame.
    // Resets soundHasPlayed so the surprise sound can replay if the object moves again.
    public void OnObjectMovingStopped()
    {
        soundHasPlayed = false;
    }

    // Called when the same object that was already moving is still moving.
    // Handles the case where the object stopped and started again (cooldown + soundHasPlayed check).
    public void OnSameObjectStillMoving(GameObject movingObject)
    {
        //StopNonSuspiciousSound();
        HandleSurpriseSound(movingObject);
    }

    // Called when an investigation starts.
    // Stops the non suspicious sound and plays the curious reaction.
    public void OnInvestigationStarted()
    {
        StopNonSuspiciousSound();
        curiousSoundEvent.Post(gameObject);
    }

    // Called when an investigation ends.
    // Attempts to play the non suspicious sound if conditions allow.
    public void OnInvestigationEnded()
    {
        TryPlayNonSuspiciousSound();
    }

    // Plays the non suspicious ambient sound if all conditions are met.
    // Called after investigation ends or when the NPC becomes unblocked.
    public void TryPlayNonSuspiciousSound()
    {
        if (canPlayNonSuspiciousSound != null && !canPlayNonSuspiciousSound()) return;
        if (nonSuspiciousSoundEvent != null)
            nonSuspiciousSoundEvent.Post(gameObject);
    }

    // Called when the NPC spots the player through a mirror.
    // Stops the non suspicious sound and plays the surprise reaction.
    public void OnPoltergSeen()
    {
        StopNonSuspiciousSound();
        surpriseSoundEvent.Post(gameObject);
        npcAnimMouth.SetTrigger("IsSurprised");
    }

    // Plays the surprise sound when a moving object is detected.
    // The sound plays if:
    // - It's a different object than the last one tracked (new object)
    // - It's the same object but it had stopped and the cooldown has elapsed
    private void HandleSurpriseSound(GameObject movingObject)
    {
        bool cooldownElapsed = (Time.time - lastSoundTime) >= surpriseCooldown;
        bool isNewObject = lastMovingObject != movingObject;
        bool canRepeat = !soundHasPlayed && cooldownElapsed;

        // If it's the same object and we are still in cooldown, we don't play the sound
        if (!isNewObject && !canRepeat) return;

        surpriseSoundEvent.Post(gameObject);
        npcAnimMouth.SetTrigger("IsSurprised");
        lastMovingObject = movingObject;
        soundHasPlayed = true;
        lastSoundTime = Time.time;
    }

    // Stops the non suspicious sound if it is currently playing.
    private void StopNonSuspiciousSound()
    {
        if (nonSuspiciousSoundEvent != null)
            nonSuspiciousSoundEvent.Stop(gameObject);
    }

    // Resets all sound state to initial values.
    // Called on respawn or level reset.
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
