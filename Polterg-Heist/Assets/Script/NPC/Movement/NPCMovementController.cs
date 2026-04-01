using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMovementController : MonoBehaviour
{
    [Header ("Sound variables")]
    [SerializeField] protected AK.Wwise.Event footstepsNPCSoundEvent;
    [SerializeField] protected AK.Wwise.RTPC speedNPC;
    [SerializeField] protected bool isMarble;
    private bool isWalking = false;

    [Header("NPC global movements variables")]
    [SerializeField] private float movementSpeed = 6f;
    [SerializeField] private float blindSpeed = 3f;
    private float normalSpeed;
    private SpriteRenderer npcSpriteRenderer;
    private Animator npcAnim;
    private bool canFindPath = true;
    

    BasicNPCBehaviour basicNPCBehaviour;

    // Getters
    public bool CanFindPath => canFindPath;
    public float MovementSpeed => movementSpeed;

    private void Start()
    {
        //npcSpriteRenderer = GetComponent<SpriteRenderer>();
        basicNPCBehaviour = GetComponent<BasicNPCBehaviour>();
        npcAnim = GetComponentInChildren<Animator>();
        StartCoroutine(StartRelated());
        normalSpeed = movementSpeed;
        npcAnim.SetFloat("Speed", normalSpeed / 3f);
    }

    IEnumerator StartRelated()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        npcSpriteRenderer = basicNPCBehaviour.SpriteRenderer;
    }

    // Changes the movement speed (normal/blind)
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

    public void ResetMovementSpeed()
    {
        movementSpeed = normalSpeed;
    }


    // Updates sprite direction based on movement direction
    public void UpdateSpriteDirection(Vector2 destination)
    {
        BasicNPCBehaviour npc = GetComponent<BasicNPCBehaviour>();
        
        // Flip sprite based on direction
        Vector2 npcDirection = (destination - (Vector2)transform.position).normalized;
        bool faceRight = npcDirection.x >= 0;

        if (faceRight != npc.FacingRight)
        {
            // Sprite face the right direction
            if (GetComponentInChildren<NPCSpriteManager>() == null)
            {
                npcSpriteRenderer.flipX = !faceRight;
            }

            //npcSpriteRenderer.flipX = !faceRight;
            npc.FacingRight = faceRight;
            npc.FlipFieldOfView();
        }


        ////// Sprite face the right direction
        //npcSpriteRenderer.flipX = npcDirection.x < 0;
        //npc.FacingRight = !npcSpriteRenderer.flipX;
        //npc.FlipFieldOfView();
    }

    // The NPC walks horizontally to a given destination
    protected IEnumerator HorizontalMovementToTarget(Vector2 destination)
    {
        if (!isWalking)
        {
            isWalking = true;
            //print("POSTTTTTT");
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
            // NPC walk towards the destination
            npcAnim.SetBool("InMovement", true);
            transform.position = Vector2.MoveTowards(transform.position, destination, movementSpeed * Time.deltaTime);
            speedNPC.SetValue(gameObject, movementSpeed);
            yield return null;
        }
        isWalking = false;
        npcAnim.SetBool("InMovement", false);
        footstepsNPCSoundEvent.Stop(gameObject);
    }
    // Pathfinding of the NPC to reach a target
    public IEnumerator ReachTarget(Vector2 target, float currentFloor, float targetFloor)
    {
        canFindPath = true;
        yield return ReachFloor(currentFloor, targetFloor);
        //yield return StartCoroutine(ReachFloor(targetFloor));
        if (!canFindPath)
        {
            yield break;
        }

        // The NPC is now on the same level has the object
        Vector2 destination = new Vector2(target.x, transform.position.y);
        // Flip sprite based on direction
        UpdateSpriteDirection(destination);
        // The NPC must walk to the target
        yield return HorizontalMovementToTarget(destination);
    }

    public IEnumerator ReachFloor(float currentFloor, float targetFloor)
    {
        // Check if the npc need to use the stairs
        if (currentFloor != targetFloor)
        {
            // Find a path using stairs to reach the target floor
            FloorNavigationRequest floorRequest = new FloorNavigationRequest(transform.position, currentFloor, targetFloor, npcSpriteRenderer);
            List<StairController> path = FloorNavigation.Instance.FindPathToFloor(floorRequest);

            if (path == null)
            {
                canFindPath = false;
                yield break;
            }

            foreach (StairController stair in path)
            {
                StairController currentStair = stair;
                // NPC must walk to the stair
                Vector2 stairPosition = new Vector2(currentStair.StartPoint.position.x, transform.position.y);
                // Flip sprite based on direction
                UpdateSpriteDirection(stairPosition);

                // Determine if we need to go up or down
                StairDirection stairDirection = (targetFloor > currentFloor) ? StairDirection.Upward : StairDirection.Downward;

                StairController nextStairFloor = null;
                bool upward = false;

                if (stairDirection == StairDirection.Upward)
                {
                    nextStairFloor = currentStair.UpperFloor;
                    upward = true;
                }
                else if (stairDirection == StairDirection.Downward)
                {
                    nextStairFloor = currentStair.BottomFloor;
                    upward = false;
                }
                //print("IS DIRECTION GOOD" + stairDirection);
                // Move to stair
                yield return HorizontalMovementToTarget(stairPosition);

                // Keep track of stairs we've already tried and found blocked
                List<StairController> blockedStairs = new List<StairController>();

                // Check if the stair is blocked
                if (currentStair.IsStairBlocked() || nextStairFloor.IsStairBlocked())
                {
                    print("HERE!");
                    bool findAlternative = false;
                    blockedStairs.Add(currentStair);    // Remove these stairs from the possible alternative to find a path
                    //print("TEST: " + blockedStairs.Count);
                    // As long as we did not find a new path
                    while (!findAlternative)
                    {
                        // Find if there is any other stair on the same level that can reach the target floor
                        for (int i = 0; i < FloorNavigation.Instance.StairsByFloorLevel[currentFloor].Count; i++)
                        {
                            //print("FLLOR RESQUEST CURRENT FLOOR :" + currentFloor);
                            //print("TARGET " + targetFloor);
                            FloorNavigationRequest alternativeFloorRequest = new FloorNavigationRequest(transform.position, currentFloor, targetFloor, npcSpriteRenderer);
                            StairController alternativeStair = FloorNavigation.Instance.FindNearestStairToFloor(alternativeFloorRequest, stairDirection, blockedStairs);

                            // Check if we found another stair
                            if (alternativeStair != null)
                            {
                                nextStairFloor = upward ? alternativeStair.UpperFloor : alternativeStair.BottomFloor;
                            }
                            // Check if the stair is not blocked and if the upstair or downstair door is also not blocked
                            if (alternativeStair != null && !alternativeStair.IsStairBlocked() && nextStairFloor != null && !nextStairFloor.IsStairBlocked())
                            {
                                // NPC must walk to the stair
                                Vector2 alternativeStairPosition = new Vector2(alternativeStair.StartPoint.position.x, transform.position.y);

                                // Flip sprite based on direction
                                UpdateSpriteDirection(alternativeStairPosition);

                                // Move to stair
                                yield return HorizontalMovementToTarget(alternativeStairPosition);

                                // Once the NPC reached the alternative stair check if it's blocked
                                if (!alternativeStair.IsStairBlocked())
                                {
                                    currentStair = alternativeStair;
                                    findAlternative = true;

                                    blockedStair = null;
                                    secundBlockedStair = null;
                                    if(blockedSoundEvent != null)
                                    {
                                        blockedSoundEvent.Stop(gameObject);
                                    }
                                    print("alternative stair");


                                    break;
                                }
                                else
                                {
                                    // This stair is blocked and should not be considered anymore
                                    blockedStairs.Add(alternativeStair);
                                }

                            }
                            //print("TEST2: " + blockedStairs.Count);
                        }
                        if(blockedStairs.Count == 1 && !findAlternative)
                        {
                            blockedStair = blockedStairs[0];
                            if(blockedStair.UpperFloor != null)
                            {
                                secundBlockedStair = blockedStair.UpperFloor;
                            }
                            else if(blockedStair.BottomFloor != null)
                            {
                                secundBlockedStair = blockedStair.BottomFloor;
                            }
                            blockedSoundEvent = blockedStair.npcLockedSoundEvent;
                            if(blockedSoundEvent != null)
                            {
                                blockedStair.npcLockedSoundEvent.Post(gameObject, (uint)AkCallbackType.AK_Marker, MarkerCallback);
                            }

                        }
                        else
                        {
                            //print("WEIRD " + blockedStairs.Count);
                        }
                        // All stairs are blocked so we wait and check if any of them got unblocked
                        yield return new WaitForSeconds(0.5f);
                        blockedStairs.Clear();
                        //print("CLEAR");
                    }
                }
                // Determine if we need to go up or down
                //StairDirection stairDirection = (targetFloor > floorLevel) ? StairDirection.Upward : StairDirection.Downward;
                // When the npc reached the stairs
                currentStair.ClimbStair(this.gameObject, stairDirection);

                // Wait for stair climbing to finish
                yield return new WaitForSeconds(1f);

                //Update our current floor
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

    public void Reset()
    {
        //footstepsNPCSoundEvent.Stop(gameObject);
        //StopAllCoroutines();
        //print("RESET");
        footstepsNPCSoundEvent.Stop(gameObject);
        StopAllCoroutines();
        isWalking = false;  // Reset walking state
        npcAnim.SetBool("InMovement", false);
    }

    StairController blockedStair;
    StairController secundBlockedStair;
    AK.Wwise.Event blockedSoundEvent;
    private void MarkerCallback(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info)
    {
        if (in_type == AkCallbackType.AK_Marker)
        {
            AkMarkerCallbackInfo markerInfo = (AkMarkerCallbackInfo)in_info;
            //Debug.Log("Marker Triggered: " + markerInfo.strLabel);

            // Ici tu déclenches ton animation
            Animator roomAnimator = blockedStair.stairAnimator;
            Animator secundAnimator = secundBlockedStair.stairAnimator;
            if (roomAnimator != null)
            {
                roomAnimator.SetTrigger("NPCBlocked");
                secundAnimator.SetTrigger("NPCBlocked");
            }
            //Animator roomAnimator = blockedStair.GetComponent<Animator>();
            //Animator secundAnimator = secundBlockedStair.GetComponent<Animator>();
            //if (roomAnimator != null)
            //{
            //    roomAnimator.SetTrigger("NPCBlocked");

            //}
        }
    }
}
