using System.Collections;
using UnityEngine;

public class TrapDoor : MonoBehaviour
{
    [Header ("Sound Variables")]
    [SerializeField] protected AK.Wwise.Event trapDoorOpenSoundEvent;
    [SerializeField] protected AK.Wwise.Event trapDoorCloseSoundEvent;

    [Header  ("Global variables")]
    private JointAngleLimits2D openDoorLimits;
    private JointAngleLimits2D closeDoorLimits;
    private HingeJoint2D hingeJoint2D;
    private BoxCollider2D trapCollider;
    [SerializeField] private LayerMask possessedObjectLayer;
    [SerializeField] private float minimumBlockHeight;
    [SerializeField] private float minimumBlockWidth;

    private Canvas uiPromptCanvas;  // Canvas with prompt image
    private Animator animator;  // Animator of the canvas for the prompt image
    private bool isPlayerTouchingTrap = false;
    private bool isDoorOpen = false;
    private void Awake()
    {
        hingeJoint2D = GetComponent<HingeJoint2D>();
        openDoorLimits = hingeJoint2D.limits;
        closeDoorLimits = new JointAngleLimits2D { min = 0f, max = 0f };
        trapCollider = GetComponent<BoxCollider2D>();
        CloseDoor();
        //hingeJoint2D.limits = closeDoorLimits;
        //isDoorOpen = false;
    }

    private void Start()
    {
        uiPromptCanvas = GetComponentInChildren<Canvas>();
        animator = GetComponentInChildren<Animator>(true);
        //uiPromptCanvas = this.gameObject.transform.parent.GetComponentInChildren<Canvas>();//GetComponentInChildren<Canvas>();
        //animator = this.gameObject.transform.parent.GetComponentInChildren<Animator>();
    }
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

    private IEnumerator WaitBeforeClosing()
    {
        yield return new WaitForSeconds(3f);
        CloseDoor();
    }

    private void Update()
    {
        IsTrapDoorBlocked();
    }

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
            //print("Object width " + objectWidth);
            //print("object height " + objectHeight);
            if(objectWidth >= minimumBlockWidth || objectHeight >= minimumBlockHeight)
            {
                return true;
            }
        }
        return false;
       
    }

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

    private void OnCollisionExit2D(Collision2D collision)
    {
        if ((collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PossessionController>()))
        {
            isPlayerTouchingTrap = false;
            HidePrompt();
        }
    }

    private void ShowPrompt()
    {
        uiPromptCanvas.enabled = true;
        animator.SetBool("PromptAppear", true);
    }

    private void HidePrompt()
    {
        uiPromptCanvas.enabled = false;
        animator.SetBool("PromptAppear", false);
    }
}
