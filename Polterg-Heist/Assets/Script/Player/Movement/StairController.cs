using System.Collections;
using System.Drawing;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public enum StairDirection
{
    Upward,
    Downward
}
public class StairController : MonoBehaviour
{
    // This class manage stairs depending on the direction desired by the character

    [Header("Stairs")]
    [SerializeField] private bool canPoltergUseDoor = true;
    [SerializeField] private bool canObjectUseDoor = false;
    [SerializeField] private float floorLevel;
    [SerializeField] public float FloorLevel => floorLevel;
    [SerializeField] private Transform startPoint;  // Center of the door on floor level
    //public Transform StartPoint => startPoint;
    [SerializeField] private StairController upperFloor;   // Next upper floor
    [SerializeField] private StairController bottomFloor; // Bottom floor
    //[SerializeField] private Transform nextFloor;   // Next upper floor
    //[SerializeField] private Transform bottomFloor; // Bottom floor
    [SerializeField] private float speed = 5f;      // Speed to go at the center of the door before climbing
    [SerializeField] private float maximumHeight;   // Maximum Height of the object so he can climb the stair
    [SerializeField] private float maximumWidth;    // Maximum Width of the object so he can climb the stair

    [SerializeField] private LayerMask possessedObjectLayer;    // Layer of objects that can block the stair
    [SerializeField] private float minimumBlockHeight;  // Minimum height of an object to block the door
    [SerializeField] private float minimumBlockWidth;   // Minimum width of an object to block the door
    [SerializeField] private float blockingThreshold;   // How much of the stair width needs to be blocked (0.5 = 50%)

    Collider2D stairCollider;

    [Header ("Sound bariables")]
    [SerializeField] public AK.Wwise.Event doorOpenSoundEvent;
    [SerializeField] public AK.Wwise.Event doorCloseSoundEvent;
    [SerializeField] public AK.Wwise.Event npcLockedSoundEvent;
    [SerializeField] public Animator stairAnimator;

    // Public properties to access from other scripts
    public Transform StartPoint { get { return startPoint; } }
    public StairController UpperFloor { get { return upperFloor; } }
    public StairController BottomFloor { get { return bottomFloor; } }
    public float MaximumHeight { get { return maximumHeight; } }
    public float MaximumWidth { get { return maximumWidth; } }

    private bool canCharacterJump;
    private bool canCharacterMove;

    private Canvas uiPromptCanvas;  // Canvas with prompt image
    [SerializeField]  private Animator promptAnimator;  // Animator of the canvas for the prompt image

    private void Start()
    {
        stairCollider = GetComponent<Collider2D>();
        uiPromptCanvas = GetComponentInChildren<Canvas>();
        //promptAnimator = GetComponentInChildren<Animator>();
    }
    public void ClimbStair(GameObject character, StairDirection direction)
    {
        PlayerController playerController = character.GetComponent<PlayerController>();
        if(playerController != null && !canPoltergUseDoor)
        {
            
            return; // Polterg can't use the door
        }
        PossessionController possessionController = character.GetComponent<PossessionController>();
        if(possessionController != null && !canObjectUseDoor)
        {
            //possessionController.isClimbing = false;
            return; // Possessed Object can't use the door
        }
        Renderer characterRenderer = character.GetComponentInChildren<Renderer>();
        //character.transform.GetChild(0).GetComponent<Renderer>();
        //print("Character size X: " + characterRenderer.bounds.size.x);
        //print("Character size Y: " + characterRenderer.bounds.size.y);
        // If the character fit with the stair dimension, then he can climb
        if (characterRenderer.bounds.size.x < maximumWidth && characterRenderer.bounds.size.y < maximumHeight)
        {
            if(possessionController != null )
            {
                PossessionManager possessionManager = character.GetComponent<PossessionManager>();
                if (possessionManager != null)
                {
                    if (!possessionManager.IsPossessing)
                    {
                        return;
                    }
                    possessionController.isClimbing = true;
                    //print("IS CLIMBING!!");
                }
                
            }
            StartCoroutine(HandleClimbingStair(character, direction));
        }

    }

    private IEnumerator HandleClimbingStair(GameObject character, StairDirection direction)
    {
        // Which floor does the character want to go to
        StairController targetStair = direction == StairDirection.Upward ? upperFloor : bottomFloor;
        PossessionController possessionController = character.GetComponent<PossessionController>();

        if (targetStair == null)
        {

            if (possessionController != null)
            {
                possessionController.isClimbing = false;
            }
            yield break;
        }

        // Kepp the player from moving when climbing the stair
        if (character.GetComponent<PlayerController>() != null)
        {
            canCharacterMove = character.GetComponent<PlayerController>().canMove;
            canCharacterJump = character.GetComponent<PlayerController>().canJump;
            character.GetComponent<PlayerController>().canMove = false;
            character.GetComponent<PlayerController>().canJump = false; 
        }

        // Move towards the center of the stairs
        while (Mathf.Abs(character.transform.position.x - startPoint.position.x) > 0.1f)
        {
            // We center the player with the stair before climbing it
            Vector2 targetPosition = new Vector2(startPoint.position.x, character.transform.position.y);
            character.transform.position = Vector2.MoveTowards(character.transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }
        yield return new WaitForSeconds(0.1f);

        //If it's an object
        PossessionManager possessManage = character.GetComponent<PossessionManager>();
        if(possessManage != null)
        {
            if (possessManage.isAttacked)
            {
                if (possessionController != null)
                {
                    possessionController.isClimbing = false;
                    print("Climbing false");
                }
                yield break;
            }
        }

        if(doorOpenSoundEvent != null)
        {
            doorOpenSoundEvent.Post(gameObject);
        }
        
        // Climbing stair animation

        BasicNPCBehaviour npcBehaviour = character.GetComponent<BasicNPCBehaviour>();
        if (npcBehaviour != null)
        {
            float newFloorLevel = (direction == StairDirection.Upward) ?
                UpperFloor.FloorLevel : BottomFloor.FloorLevel;
            npcBehaviour.UpdateFloorLevel(newFloorLevel);
        }


        // Calculate adjusted destination position based on character height
        Renderer characterRenderer = character.GetComponentInChildren<Renderer>();//character.transform.GetChild(0).GetComponent<Renderer>();
        float characterHeight = characterRenderer.bounds.size.y;
        Vector2 adjustedPosition = targetStair.startPoint.position;

        // Adjust Y position so the character's feet are at the floor level
        adjustedPosition.y += characterHeight / 2f;

        // Teleport the player to the adjusted position
        character.transform.position = adjustedPosition;               

        // Finish climbing stair animation

        // Re-enable player movement
        if (character.GetComponent<PlayerController>() != null)
        {
            character.GetComponent<PlayerController>().canMove = canCharacterMove;
            character.GetComponent<PlayerController>().canJump = canCharacterJump;
        }

        if (possessionController != null)
        {
            //print("ISCLIMBINGFASLSE");
            possessionController.isClimbing = false;
        }
        else
        {
            //print("NO POSSESSIONCONTROLLER");
        }

        if(doorCloseSoundEvent != null)
        {
            doorCloseSoundEvent.Post(gameObject);
        }
        
    }

    // Check if there is a possessed object in front of the stair
    public bool IsStairBlocked() 
    {  
        // Get the sprite bounds
        Bounds stairBounds = stairCollider.bounds;

        // Get stair width and height
        float stairWidth = stairBounds.size.x;
        float stairHeight = stairBounds.size.y;

        // Get object colliders in front of the stair
        Collider2D[] colliders = Physics2D.OverlapBoxAll(stairCollider.bounds.center,
                                                        new Vector2(stairWidth, stairHeight),
                                                        0f, possessedObjectLayer
                                                        );
        float blockedWidth = 0f;

        foreach (Collider2D collider in colliders)
        {
            // Skip if the object is not tall enough
            if (collider.bounds.size.y < minimumBlockHeight)
                continue;

            // Calculate how much width of the room is the object taking
            float objectWidth = Mathf.Min(collider.bounds.max.x, stairBounds.max.x)
                                - Mathf.Max(collider.bounds.min.x, stairBounds.min.x);
            if (objectWidth > 0)
            {
                blockedWidth += objectWidth;
            }
        }

        // Calculate what percentage of the entrance is blocked
        float blockPercentage = blockedWidth / stairWidth;
        return blockPercentage >= blockingThreshold;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        DisplayUIPrompt(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        DisplayUIPrompt(collision);
    }

    private void DisplayUIPrompt(Collider2D collision)
    {
        // Make Climb Stairs prompt appear if Polterg is in front of a stair
        if (collision.CompareTag("Player") && !IsStairBlocked() && canPoltergUseDoor && (BottomFloor != null || UpperFloor != null))
        {
            if (uiPromptCanvas != null)
            {
                uiPromptCanvas.enabled = true;
                promptAnimator.SetBool("PromptAppear", true);
            }

        }
        // Make Climb Stairs prompt appear if Polterg is in front of a stair in a possessedObject
        else if (collision.gameObject.GetComponent<PossessionController>())
        {
            PossessionManager possessionObject = collision.gameObject.GetComponent<PossessionManager>();
            if (possessionObject != null && !IsStairBlocked() && possessionObject.IsPossessing && canObjectUseDoor && (BottomFloor != null || UpperFloor != null))
            {
                if (uiPromptCanvas != null && (collision.bounds.size.y < minimumBlockHeight))
                {
                    uiPromptCanvas.enabled = true;
                    promptAnimator.SetBool("PromptAppear", true);
                }

            }
        }
        else
        {
            HideUIPrommpt(collision);
        }
    }

    private void HideUIPrommpt(Collider2D collision)
    {
        // Make Climb Stairs prompt disappear if Polterg is in front of a stair
        if (collision.CompareTag("Player"))
        {
            if (uiPromptCanvas != null)
            {
                uiPromptCanvas.enabled = false;
                promptAnimator.SetBool("PromptAppear", false);
            }

        }
        // Make Climb Stairs prompt disappear if Polterg is in front of a stair in a possessedObject
        else if (collision.gameObject.GetComponent<PossessionController>())
        {
            PossessionManager possessionObject = collision.gameObject.GetComponent<PossessionManager>();
            if (possessionObject != null && possessionObject.IsPossessing)
            {
                if (uiPromptCanvas != null)
                {
                    uiPromptCanvas.enabled = false;
                    promptAnimator.SetBool("PromptAppear", false);
                }

            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        HideUIPrommpt(collision);
    }
}
