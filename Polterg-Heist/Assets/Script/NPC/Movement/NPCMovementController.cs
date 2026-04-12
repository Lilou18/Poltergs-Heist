using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMovementController : MonoBehaviour
{
    [Header ("Sound")]
    [SerializeField] protected AK.Wwise.Event footstepsNPCSoundEvent;   // Sound Event fired when Human NPC is walking
    [SerializeField] protected AK.Wwise.RTPC speedNPC;                  // RTPC controlling NPC movement speed parameter in audio engine
    [SerializeField] protected bool isMarble;                           // If true, plays marble footstep sounds instead of wood

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 6f;                  
    [SerializeField] private float blindSpeed = 3f;                     // Reduced movement speed used when the NPC is blinded

    private float normalSpeed;                                          // Stores the original movement speed for restoration after blind mode
    private bool isWalking = false;                                     // True while the NPC is actively walking
    private bool canFindPath = true;                                    // False if no valid stair path was found during floor navigation

    private SpriteRenderer npcSpriteRenderer;                           // Sprite renderer of the NPC
    private Animator npcAnim;                                           // Animator controlling NPC movement animations
    private BasicNPCBehaviour npc;                                      // Reference to the NPC behaviour component for direction and sprite management

    // Blocked stair tracking
    private StairController blockedStair;                               // Blocked stair in the NPC path
    private StairController secundBlockedStair;                         // Stair linked to the blocked stair on another floor level
    private AK.Wwise.Event blockedSoundEvent;                           // Sound event fired when an NPC is blocked

    // Getters
    public bool CanFindPath => canFindPath;

    private void Start()
    {
        npc = GetComponent<BasicNPCBehaviour>();
        npcSpriteRenderer = npc.SpriteRenderer;
        npcAnim = GetComponentInChildren<Animator>();
        normalSpeed = movementSpeed;
        npcAnim.SetFloat("Speed", normalSpeed / 3f);
    }

    // Moves the NPC to the target position, navigating floors via staircases if needed.
    // Sets canFindPath to false if no valid path exists.
    public IEnumerator ReachTarget(Vector2 target, float currentFloor, float targetFloor)
    {
        canFindPath = true;

        // Step 1: Navigate to the correct floor if needed
        yield return ReachFloor(currentFloor, targetFloor);

        // Abort if no valid path was found during floor navigation
        if (!canFindPath)
        {
            yield break;
        }

        // The NPC is now on the same floor level has the target
        // Step 2: Move horizontally to the target on the correct floor
        Vector2 destination = new Vector2(target.x, transform.position.y);        
        UpdateFacingDirection(destination);
        yield return HorizontalMovementToTarget(destination);
    }

    // Navigates the NPC to the target floor using the staircase system.
    // Handles blocked stairs by finding and waiting for alternatives.
    public IEnumerator ReachFloor(float currentFloor, float targetFloor)
    {
        // Check if the npc need to use the stairs
        if (currentFloor != targetFloor)
        {
            // Find a path using the floor navigation system to reach the targeted floor
            FloorNavigationRequest floorRequest = new FloorNavigationRequest(transform.position, currentFloor, targetFloor, npcSpriteRenderer);

            // List of stairs to take to reach the target floor
            List<StairController> path = FloorNavigation.Instance.FindPathToFloor(floorRequest);

            // Abort if no valid path was found during floor navigation
            if (path == null)
            {
                canFindPath = false;
                yield break;
            }

            foreach (StairController stair in path)
            {
                StairController currentStair = stair;
                // Determine if we need to go up or down
                StairDirection stairDirection = (targetFloor > currentFloor) ? StairDirection.Upward : StairDirection.Downward;

                bool upward = stairDirection == StairDirection.Upward;
                StairController nextStairFloor = upward ? currentStair.UpperFloor : currentStair.BottomFloor;

                // Walk to the stair entrance
                Vector2 stairPosition = new Vector2(currentStair.StartPoint.position.x, transform.position.y);                
                UpdateFacingDirection(stairPosition);
                yield return HorizontalMovementToTarget(stairPosition);

                // If the stair or its linked stair on the other floor is blocked, the NPC searches for another stair on the same floor
                // that can lead to the target floor.
                if (currentStair.IsStairBlocked() || nextStairFloor.IsStairBlocked())
                {
                    // Track stairs already tried to avoid retrying them
                    List<StairController> blockedStairs = new List<StairController>();
                    bool findAlternative = false;
                    blockedStairs.Add(currentStair);    // Remove these stairs from the possible alternative to find a path

                    // As long as we do not find a new path
                    while (!findAlternative)
                    {
                        // Iterate over all stairs on the current floor to find a valid alternative
                        for (int i = 0; i < FloorNavigation.Instance.StairsByFloorLevel[currentFloor].Count; i++)
                        {
                            FloorNavigationRequest alternativeFloorRequest = new FloorNavigationRequest(transform.position, currentFloor, targetFloor, npcSpriteRenderer);
                            // Find the nearest unblocked stair, excluding already tried ones
                            StairController alternativeStair = FloorNavigation.Instance.FindNearestStairToFloor(alternativeFloorRequest, stairDirection, blockedStairs);

                            if (alternativeStair == null) continue;

                            // Determine the linked stair on the destination floor.
                            nextStairFloor = upward ? alternativeStair.UpperFloor : alternativeStair.BottomFloor;

                            // Only consider stairs where both the stair and its linked stair on the other floor are unblocked
                            if (!alternativeStair.IsStairBlocked() && nextStairFloor != null && !nextStairFloor.IsStairBlocked())
                            {
                                // NPC walk to the alternative stair found
                                Vector2 alternativeStairPosition = new Vector2(alternativeStair.StartPoint.position.x, transform.position.y);                                                               
                                UpdateFacingDirection(alternativeStairPosition);
                                yield return HorizontalMovementToTarget(alternativeStairPosition);

                                // Once the NPC reaches the alternative stair, check if they have become blocked in the meantime
                                if (!alternativeStair.IsStairBlocked())
                                {
                                    currentStair = alternativeStair;
                                    findAlternative = true;

                                    // Clear blocked stair tracking state and stop the blocked sound
                                    blockedStair = null;
                                    secundBlockedStair = null;
                                    if(blockedSoundEvent != null)
                                    {
                                        blockedSoundEvent.Stop(gameObject);
                                    }
                                    break;
                                }
                                else
                                {
                                    // Stair was blocked after we walked to it. We now add it to exclusion list
                                    blockedStairs.Add(alternativeStair);
                                }

                            }
                        }
                        // No alternative found. Register the blocked stair and play its blocked sound and animation
                        if (blockedStairs.Count == 1 && !findAlternative)
                        {                            
                            blockedStair = blockedStairs[0];

                            // Get the linked stair on the other floor.
                            secundBlockedStair = blockedStair.UpperFloor ?? blockedStair.BottomFloor;

                            // Store and trigger the blocked stair sound event
                            blockedSoundEvent = blockedStair.npcLockedSoundEvent;
                            if(blockedSoundEvent != null)
                            {
                                // Play sound with marker callback to sync animation feedback
                                blockedStair.npcLockedSoundEvent.Post(gameObject, (uint)AkCallbackType.AK_Marker, MarkerCallback);
                            }

                        }
                        // All stairs are blocked. Wait before retrying and check if any of them got unblocked
                        yield return new WaitForSeconds(0.5f);
                        blockedStairs.Clear();
                    }
                }
                // If the stair is not blocked, climb it and wait for the animation to complete
                currentStair.ClimbStair(this.gameObject, stairDirection);    
                yield return new WaitForSeconds(1f);

                // Update the current floor after climbing
                if (stairDirection == StairDirection.Upward)
                {
                    currentFloor = currentStair.UpperFloor.FloorLevel;
                }
                else if (stairDirection == StairDirection.Downward)
                {
                    currentFloor = currentStair.BottomFloor.FloorLevel;
                }
            }
        }

    }

    // Set NPC orientation based on movement direction.
    private void UpdateFacingDirection(Vector2 destination)
    {
        Vector2 direction = (destination - (Vector2)transform.position).normalized;
        bool faceRight = direction.x >= 0;
        npc.SetFacingDirection(faceRight);
    }

    // Moves the NPC horizontally toward the target one frame at a time.
    // Handles footstep sound and walking animation.
    protected IEnumerator HorizontalMovementToTarget(Vector2 destination)
    {
        if (!isWalking)
        {
            isWalking = true;
            footstepsNPCSoundEvent?.Post(gameObject);
            if (isMarble)
            {
                AkUnitySoundEngine.SetSwitch("Material", "Marble", gameObject);
            }
            else
            {
                AkUnitySoundEngine.SetSwitch("Material", "Wood", gameObject);
            }

        }

        // The NPC must walk until it reaches its destination
        while (Mathf.Abs(transform.position.x - destination.x) > 0.1f)
        {
            npcAnim.SetBool("InMovement", true);
            transform.position = Vector2.MoveTowards(transform.position, destination, movementSpeed * Time.deltaTime);
            speedNPC.SetValue(gameObject, movementSpeed);
            yield return null;
        }
        isWalking = false;
        npcAnim.SetBool("InMovement", false);
        footstepsNPCSoundEvent.Stop(gameObject);
    }

    // Wwise marker callback triggered during the blocked stair sound event.
    // Plays the blocked animation on the stair and its linked stair on another floor.
    private void MarkerCallback(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info)
    {
        if (in_type == AkCallbackType.AK_Marker)
        {
            AkMarkerCallbackInfo markerInfo = (AkMarkerCallbackInfo)in_info;

            Animator roomAnimator = blockedStair.stairAnimator;
            Animator secundAnimator = secundBlockedStair.stairAnimator;
            if (roomAnimator != null)
            {
                roomAnimator.SetTrigger("NPCBlocked");
                secundAnimator.SetTrigger("NPCBlocked");
            }
        }
    }


    // Toggles between normal and blind movement speed.
    // Also updates the animator speed parameter.
    public void ChangeSpeed()
    {
        if (movementSpeed != normalSpeed)
        {
            movementSpeed = normalSpeed;
        }
        else
        {
            movementSpeed = blindSpeed;
        }
        npcAnim.SetFloat("Speed", movementSpeed / 3f);
    }

    // Resets movement speed back to normal.
    public void ResetMovementSpeed()
    {
        movementSpeed = normalSpeed;
    }

    // Stops movement sound and animation.
    public void StopMovement()
    {
        footstepsNPCSoundEvent.Stop(gameObject);
        isWalking = false;
        npcAnim.SetBool("InMovement", false);
    }

    // Stops all movement coroutines and resets animation and sound state.
    public void Reset()
    {
        footstepsNPCSoundEvent.Stop(gameObject);
        StopAllCoroutines();
        isWalking = false;
        npcAnim.SetBool("InMovement", false);
    }
}
