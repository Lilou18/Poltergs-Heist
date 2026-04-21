using System.Collections;
using UnityEngine;

public class TrapDoor : MonoBehaviour
{
    // Manages a trap door that can be opened by the player pressing S while touching it.
    // The door opens via a HingeJoint2D and closes automatically after a delay.
    // Blocked by possessed objects that are wide or tall enough to obstruct the opening.
    // Shows a UI prompt when the player is touching the door and it is closed.

    [Header ("Sound")]
    [SerializeField] protected AK.Wwise.Event trapDoorOpenSoundEvent;   // Played when the door opens
    [SerializeField] protected AK.Wwise.Event trapDoorCloseSoundEvent;  // Played when the door closes

    [Header("Blocking Detection")]
    [SerializeField] private LayerMask possessedObjectLayer;            // Possessed object layer that can block the trap
    [SerializeField] private float minimumBlockHeight;                  // Minimum object height required to block the door
    [SerializeField] private float minimumBlockWidth;                   // Minimum object width required to block the door

    private HingeJoint2D hingeJoint2D;                                  // Controls the door rotation via angle limits
    private BoxCollider2D trapCollider;                                 // Used to detect blocking objects above the door
    private Canvas uiPromptCanvas;                                      // Canvas containing the interaction prompt image
    private Animator animator;                                          // Controls the prompt appearance animation

    private JointAngleLimits2D openDoorLimits;                          // Hinge limits when the door is open
    private JointAngleLimits2D closeDoorLimits;                         // Hinge limits when the door is closed
    private bool isPlayerTouchingTrap = false;                          // True while the player or a possessed object is in contact with the door
    private bool isDoorOpen = false;                                    // True while the door is in the open state

    private void Awake()
    {
        hingeJoint2D = GetComponent<HingeJoint2D>();
        // Save the open limits from the inspector before overriding them
        openDoorLimits = hingeJoint2D.limits;
        closeDoorLimits = new JointAngleLimits2D { min = 0f, max = 0f };
        trapCollider = GetComponent<BoxCollider2D>();
        CloseDoor();
    }

    private void Start()
    {
        uiPromptCanvas = GetComponentInChildren<Canvas>();
        animator = GetComponentInChildren<Animator>(true);
    }

    // Opens the door, plays the open sound, hides the prompt, and starts the auto close timer.
    public void OpenDoor()
    {
        hingeJoint2D.limits = openDoorLimits;
        if (!isDoorOpen)
        {
            trapDoorOpenSoundEvent.Post(gameObject);
        }
        isDoorOpen = true;
        HidePrompt();
        StartCoroutine(WaitBeforeClosing());
    }

    // Closes the door by resetting the hinge limits to zero.
    // Plays the close sound and restores the prompt if the player is still touching the door.
    private void CloseDoor()
    {
        hingeJoint2D.limits = closeDoorLimits;
        if (isDoorOpen)
        {
            trapDoorCloseSoundEvent.Post(gameObject);
        }
        isDoorOpen = false; 
        if (isPlayerTouchingTrap)
        {
            ShowPrompt();
        }
    }

    // Waits before automatically closing the door after it has been opened.
    private IEnumerator WaitBeforeClosing()
    {
        yield return new WaitForSeconds(3f);
        CloseDoor();
    }

    // Returns true if any possessed object above the door is large enough to block it.
    public bool IsTrapDoorBlocked()
    {
        // Get the collider bounds
        Bounds trapBounds = trapCollider.bounds;

        // Get trap width and height
        float trapWidth = trapBounds.size.x;
        float detectionHeight = trapBounds.size.y;

        // Get object colliders in front of the stair
        Collider2D[] colliders = Physics2D.OverlapBoxAll(trapBounds.center,
                                                        new Vector2(trapWidth, detectionHeight),
                                                        0f, possessedObjectLayer
                                                        );
        foreach (Collider2D collider in colliders)
        {
            float objectWidth = collider.bounds.size.x;
            float objectHeight = collider.bounds.size.y;

            // If the object on top of the trap is big enough then the trap is blocked
            if(objectWidth >= minimumBlockWidth || objectHeight >= minimumBlockHeight)
            {
                return true;
            }
        }
        return false;
       
    }

    // Shows the interaction prompt when the player or a possessed object touches the trap.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PossessionController>()) && !IsTrapDoorBlocked())
        {
            isPlayerTouchingTrap = true;
            if (!isDoorOpen)
            {
                ShowPrompt();
            }
        }
    }

    // Hides the interaction prompt when the player or possessed object leaves the trap.
    private void OnCollisionExit2D(Collision2D collision)
    {
        if ((collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PossessionController>()))
        {
            isPlayerTouchingTrap = false;
            HidePrompt();
        }
    }

    // Shows the interaction prompt above the door.
    private void ShowPrompt()
    {
        uiPromptCanvas.enabled = true;
        animator.SetBool("PromptAppear", true);
    }

    // Hides the interaction prompt.
    private void HidePrompt()
    {
        uiPromptCanvas.enabled = false;
        animator.SetBool("PromptAppear", false);
    }
}
