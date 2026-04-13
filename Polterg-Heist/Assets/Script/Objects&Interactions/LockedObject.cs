using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor.PackageManager;
using UnityEngine;

public class LockedObject : InteractibleManager
{
    // Manages a lockable object (e.g. a door) that can be unlocked and opened.
    // Plays a locked sound when the player collides while locked.
    // Plays an unlock animation and enables passage when unlocked.
    // Can be locked and unlocked externally by KeyItemBehavior.

    [SerializeField] protected AK.Wwise.Event lockedDoorSoundEvent; // Played when the player collides with a locked door
    [SerializeField] protected AK.Wwise.Event doorUnlockSoundEvent; // Played when the door is unlocked and opens

    private Collider2D objCollider;                                 // Switched to trigger when the door opens to allow passage
    private Animator animator;                                      // Controls the unlock animation

    private bool isLocked = true;                                   // True while the door requires a key to open
    private bool isOpen = false;                                    // True once the unlock animation has started

    protected void Start()
    {
        objCollider = GetComponent<Collider2D>();
        animator = GetComponentInChildren<Animator>();
    }

    // Reacts when a collider hits the door.
    // Plays a locked sound if the door is locked, opens it if unlocked.
    // Ignores non-player colliders.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsPlayer(collision.gameObject)) return;

        // Prevent multiple call
        if (isOpen) return;

        if (isLocked)
        {
            lockedDoorSoundEvent.Post(gameObject);
        }
        else
        {
            // Animation lock open up
            StartCoroutine(UnlockDoorAnimation());
        }
    }

    // Returns true if the colliding object is the player directly
    // or a possessed object currently controlled by the player.
    private bool IsPlayer(GameObject obj)
    {
        PlayerController player = obj.GetComponent<PlayerController>();
        PossessionManager possession = obj.GetComponent<PossessionManager>();
        return player != null || (possession != null && possession.IsPossessing);
    }

    // Locks the door and resets its visual and physical state.
    // Called by KeyItemBehavior.ResetInitialState on level reset.
    public void Lock()
    {
        SetLocked(true);
        ResetObjectCollider();
    }

    // Unlocks the door so the next player collision will open it.
    // Called by KeyItemBehavior.OnPickedUp when the key is collected.
    public void Unlock()
    {
        SetLocked(false);
    }

    // Updates the locked state only if it has changed.
    private void SetLocked(bool value)
    {
        if (isLocked == value) return;

        isLocked = value;
    }

    // Plays the unlock animation and converts the collider to a trigger to allow passage.
    private IEnumerator UnlockDoorAnimation()
    {
        isOpen = true;

        animator.SetBool("IsUnlock", true);
        doorUnlockSoundEvent.Post(gameObject);
        yield return new WaitForSeconds(0.5f);
        objCollider.isTrigger = true;
    }

    // Resets the door collider and animation to the closed/locked state.
    private void ResetObjectCollider()
    {
        animator.SetBool("IsUnlock", false);
        objCollider.isTrigger = false;        
        isOpen = false;
    }
}
