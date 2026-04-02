using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// But: Control the player or an object movement
/// Requiert: Rigidbody2D
/// Input: A = gauche, D = droit, SPACE = saut
/// �tat: Optimal(temp)
[RequireComponent(typeof(Rigidbody2D))]
public abstract class MovementController : MonoBehaviour
{
    //Delegates
    public delegate void Callback(string parameter);
    [HideInInspector] public string jumpParam = "jump";
    [HideInInspector] public string landParam = "land";
    [HideInInspector] public string bonkLeftParam = "bonkL";
    [HideInInspector] public string bonkRightParam = "bonkR";
    [HideInInspector] public string cantWalkLeftParam = "shakeL";
    [HideInInspector] public string cantWalkRightParam = "shakeR";

    //Events
    public event Callback onJump;
    public event Callback onLand;
    public event Callback onBonkL;
    public event Callback onBonkR;
    public event Callback onShakeL;
    public event Callback onShakeR;

    //Mouvement
    [Header("Mouvement variables")]
    [SerializeField] public float speed;                                //Mouvement speed
    [SerializeField] public float maxSpeed;                             
    [SerializeField] public float stopSpeed;                            
    [SerializeField] public float jumpSpeed;                            //Jump impulsion force
    [SerializeField][HideInInspector] public int horizontalDirection;
    private bool playerInputEnable;
    Coroutine jumpReset;
    Coroutine stairDelay;
    public float inputBuffer;
    protected Vector2 moveInput;
    Vector2 lastInput;
    float velocityJumpCap;
    public float jumpBufferTest = 0.5f;
    public bool isAPlane = false;

    //Contacts
    [Header("GameObjets in contact")]
    
    [HideInInspector] public List<GameObject> curObject = new List<GameObject>();
    [HideInInspector] public List<ContactPoint2D> curContact = new List<ContactPoint2D>();
    [HideInInspector] public float halfSizeOfObject;
    Vector2 lastVelocity;
    float lastVelocityX { get { return lastVelocity.x; } }
    float lastVelocityY { get { return lastVelocity.y; } }
    PhysicsMaterial2D objMat;
    //[HideInInspector] public float lastVelocityX;
    float lastPosX;
    [HideInInspector] public float lastPosY;
    ContactFilter2D filter = new ContactFilter2D();
    float objBounciness;

    //Conditions
    [Header("Mouvement conditions")]
    public bool canMove;                                                //Can stop all lines that use physics
    public bool canWalk;                                                //Can stop the physics using the x axis
    public bool canJump;                                                //Can stop the physics using the y axis
    public bool isInContact;                                            //To know if the object is in contact with another at the bottom
    public bool isJumping;                                              //To stop multijump.
    private bool isJumpCdFinished;
    private bool isPreJumpBufferFinished = true;
    private bool isPerformingJump = false;
    private bool canClimbAgain;

    [Header ("Sound variables")]
    [SerializeField] public AK.Wwise.Event movementXAxisSoundEvent;
    [SerializeField] public AK.Wwise.Event jumpSoundEvent;
    [SerializeField] public AK.Wwise.Event fallSoundEvent;
    [SerializeField] public AK.Wwise.Event jumpBounceSoundEvent;
    [SerializeField] public AK.Wwise.Event moveFailSoundEvent;
    protected bool isMovementXAxisSoundOn = false;
    //private float stopThresholdSound = 0.05f;

    // Ball
    private float bounceForce = 0.6f;

    //Shortcuts
    protected Rigidbody2D rigid2D;
    Collider2D col2D;

    //Input action section, has to be public or can be private with a SerializeField statement
    [Header("Input Section")]
    public InputAction move;
    public InputAction jump;

    //Debug related
    [SerializeField] float highestY;

    protected virtual void Awake()
    {
        playerInputEnable = true;
        moveInput = Vector2.zero;
        lastInput = Vector2.zero;
        canClimbAgain = true;
        isJumpCdFinished = true;
        inputBuffer = .3f;
        halfSizeOfObject = (transform.lossyScale.y * gameObject.GetComponent<Collider2D>().bounds.size.y) / 2;

        move.Enable();
        jump.Enable();
    }

    private void OnEnable()
    {
        stairDelay = null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    protected virtual void Start()
    {
        rigid2D = gameObject.transform.GetComponent<Rigidbody2D>();
        col2D = this.GetComponent<Collider2D>();
        if (rigid2D.sharedMaterial != null) 
        {
            objMat = rigid2D.sharedMaterial;
            objBounciness = objMat.bounciness; 
        }
        else if (col2D.sharedMaterial != null) 
        {
            objMat = col2D.sharedMaterial;
            objBounciness = objMat.bounciness; 
        }
        else { objBounciness = 0; }
        highestY = transform.position.y;

        if (objMat != null && objMat.name == "Ballss")
        {
            objBounciness = bounceForce;
        }
        velocityJumpCap = 8.5f;
    }

    //Physics
    protected virtual void FixedUpdate()
    {
        if (canMove)
        {
            if (lastInput.x != moveInput.x && moveInput.x != 0) { rigid2D.linearVelocityX = 0; }

            //Mouvement on the x axis
            if (canWalk)
            {
                rigid2D.linearVelocityX = rigid2D.linearVelocityX + moveInput.x * speed;
                if(movementXAxisSoundEvent != null && isInContact && !isMovementXAxisSoundOn)
                {
                    //print("MAYBE");
                    isMovementXAxisSoundOn = true;
                    movementXAxisSoundEvent.Post(gameObject);
                }
                else if(isAPlane && movementXAxisSoundEvent != null && !isMovementXAxisSoundOn)
                {
                    //print("HELLO");
                    isMovementXAxisSoundOn = true;
                    movementXAxisSoundEvent.Post(gameObject);
                }
                
            }

            //Caps the speed according to maxspeed
            rigid2D.linearVelocityX = Mathf.Clamp(rigid2D.linearVelocityX, -maxSpeed, maxSpeed);

            //Slows the x mouvement if no x input.
            if (moveInput.x == 0)
            { 
                rigid2D.linearVelocityX = rigid2D.linearVelocityX / stopSpeed;
            }

            if (isJumping && isInContact && !isPerformingJump)
            {
                StartCoroutine(PostJumpBuffer());
                isPerformingJump = true;
                if (onJump != null) { onJump(jumpParam); };
                float _jumpSpeed = jumpSpeed + Mathf.Abs(lastVelocityY * objBounciness * 1.5f);
                //print("SPEED" + _jumpSpeed);
                rigid2D.linearVelocityY = _jumpSpeed;
                if (jumpSoundEvent != null)
                {
                    //print("IS JUMPING " + isJumping);
                    jumpSoundEvent.Post(gameObject);
                }
                //rigid2D.AddForceY(jumpSpeed, ForceMode2D.Impulse);
                StartCoroutine(InputReset());
            }

            rigid2D.linearVelocityY = Mathf.Clamp(rigid2D.linearVelocityY, -velocityJumpCap, velocityJumpCap);

            lastInput = moveInput;

            
        }
        // Keep it from moving
        else
        {
            rigid2D.linearVelocityX = 0;
        }

        if (((lastPosX == rigid2D.position.x) && isInContact) || !isInContact) /*(Mathf.Abs(rigid2D.linearVelocityX) <= stopThresholdSound &&*/
        {
            if (movementXAxisSoundEvent != null && !isAPlane)
            {
                isMovementXAxisSoundOn = false;
                movementXAxisSoundEvent.Stop(gameObject);
            }

        }

        if(isAPlane && (lastPosX == rigid2D.position.x))
        {
            isMovementXAxisSoundOn = false;
            movementXAxisSoundEvent.Stop(gameObject);
        }

            lastPosX = rigid2D.position.x;
    }

    //To get the inputs
    protected virtual void Update()
    {
        if (playerInputEnable)
        {
            // The player has to release the W or S key to climb stairs again
            if (Keyboard.current.wKey.wasReleasedThisFrame || Keyboard.current.sKey.wasReleasedThisFrame)
            {
                canClimbAgain = true;
            }

            // Move left or right using A or D key
            if (move.IsPressed())
            {
                moveInput = move.ReadValue<Vector2>();

                if (canWalk)
                {
                    moveInput.x = Mathf.Round(moveInput.x);
                    moveInput.y = Mathf.Round(moveInput.y);
                }
                // For walk fail animation
                else if (canMove && move.WasPressedThisFrame())
                {
                    if (moveInput.x > 0 && onShakeR != null) { onShakeL(cantWalkRightParam); }
                    else if (moveInput.x < 0 && onShakeL != null) { onShakeR(cantWalkLeftParam); }
                }
            }
            else { moveInput = Vector2.zero; }

            // Jump using the Spacebar key
            if (jump.IsPressed())//.WasPressedThisFrame())
            {
                if (canMove)
                {
                    if (canJump && isJumpCdFinished)
                    {
                        
                        if (jumpReset != null) { StopCoroutine(jumpReset); }
                        jumpReset = StartCoroutine(PreJumpBuffer());
                    }
                    else if (!isJumping && onJump != null && jump.WasPressedThisFrame())
                    { 
                        onJump(jumpParam);
                        moveFailSoundEvent.Post(gameObject);
                    
                    };
                }

                /*if (!isJumping)
                {
                    if (canMove)
                    {
                        if (canJump)
                        {
                            //isJumping = true;
                            if (jumpReset != null) { StopCoroutine(jumpReset); }
                            jumpReset = StartCoroutine(PreJumpBuffer());
                        }
                        else
                            if (onJump != null) { onJump(jumpParam); };
                    }
                }*/
            }
        }

        if (rigid2D.linearVelocityY <= .1f && rigid2D.linearVelocityY >= -.1f) { lastPosY = this.transform.position.y; }
        if (transform.position.y > highestY)
        {
            highestY = transform.position.y;
        }

        lastVelocity = rigid2D.linearVelocity;
        //lastVelocityX = Mathf.Abs(rigid2D.linearVelocityX);
    }

    //To wipe memory of a jump input after a certain time
    IEnumerator InputReset()
    {
        // Wait a few physics frames to ensure the jump force has been applied
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        isInContact = false;
        isPerformingJump = false;
    }

    //To permit frame contact jumps for bouncy objects
    IEnumerator PostJumpBuffer()
    {
        //print("TEST");
        isJumpCdFinished = false;
        yield return new WaitForSecondsRealtime(jumpBufferTest);
        isJumpCdFinished = true;
    }

    //To permit frame contact jumps
    IEnumerator PreJumpBuffer()
    {
        float targetTime = Time.time + .1f;
        isPreJumpBufferFinished = false;

        while (targetTime >  Time.time)
        {
            isJumping = true;
            yield return null;
        }
        isPreJumpBufferFinished = true;
    }

    IEnumerator StairClimbDelay()
    {
        yield return new WaitForSecondsRealtime(.5f);
        stairDelay = null;
    }

    //Stock les GameObjets en contact avec l'objet
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        curContact.Clear();
        Physics2D.GetContacts(col2D, collision.collider, filter.NoFilter(), curContact);
        if (canMove)
        {
            //print("TEST1");
            float bounce;
            if (collision.collider.sharedMaterial != null)
            {
                if (objMat != null) 
                { 
                    bounce = objMat.bounciness;
                    if (collision.collider.sharedMaterial.name == "Bounce")
                    {
                        velocityJumpCap = 9f;
                        //print("JUMPBOUNCE");
                        
                        if (jumpBounceSoundEvent != null)
                        {
                            jumpBounceSoundEvent.Post(gameObject);
                        }
                    }
                    else
                    {
                        velocityJumpCap = 8.5f;
                    }


                }
                else { bounce = 0; }
                objBounciness = Mathf.Max(collision.collider.sharedMaterial.bounciness, bounce);
            }
            else if (collision.transform.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb2d))
            {
                if (rb2d.sharedMaterial != null)
                {
                    if (objMat != null)
                    {
                        bounce = objMat.bounciness;
                        if (rb2d.sharedMaterial.name == "Bounce")
                        {
                            velocityJumpCap = 9f;
                            //print("JUMPBOUNCE");
                            if (jumpBounceSoundEvent != null)
                            {
                                jumpBounceSoundEvent.Post(gameObject);
                            }
                        }
                        else
                        {
                            velocityJumpCap = 8.5f;
                        }

                    }
                    else { bounce = 0; }
                    objBounciness = Mathf.Max(rb2d.sharedMaterial.bounciness, bounce);
                }
            }
            else
            {
                if (objMat != null) { objBounciness = objMat.bounciness; }
                else { objBounciness = 0; }
            }

           
            
        }
        
        for (int i = 0; i < curContact.Count; i++)
        {
            if (curContact[i].normal.y <= -.9f)
            {
                
                isInContact = true;

                if (isPreJumpBufferFinished) { isJumping = false; }
                if (lastPosY - this.transform.position.y > .2f && onLand != null) { onLand(landParam); }

                if (objMat != null && objMat != null && objMat.name == "Ballss")
                {
                    rigid2D.linearVelocityY = -lastVelocityY * bounceForce;
                    objBounciness = bounceForce;
                }

                if (fallSoundEvent != null && lastPosY - transform.position.y > 0.2f)
                {
                    fallSoundEvent.Stop(gameObject);
                    fallSoundEvent.Post(gameObject);
                }

                break;
            }
            
            if(curContact[i].normal.y >= 0.9f && objMat!= null && objMat.name == "Ballss")
            {
                rigid2D.linearVelocityY = -lastVelocityY * bounceForce;
                objBounciness = bounceForce;
            }
        }

        for (int i = 0; i < curContact.Count; i++)
        {
            if (canMove && Mathf.Abs(lastVelocityX) >= maxSpeed - .1f)
            {
                if (collision.GetContact(i).normal.x <= -.9f)
                {
                    if (onBonkR != null) { onBonkR(bonkRightParam); }
                    break;
                }
                else if (collision.GetContact(i).normal.x >= .9f)
                {
                    if (onBonkL != null) { onBonkL(bonkLeftParam); }
                    break;
                }
            }
        }
    }

    

    //
    void OnCollisionExit2D(Collision2D collision)
    {
        //curObject.Remove(collision.gameObject);//if (jumpBuff != null) { StopCoroutine(PostJumpBuffer()); }
        //if (this.gameObject.activeInHierarchy && jumpBuff != null) { StopCoroutine(jumpBuff); }
        //if (this.gameObject.activeInHierarchy && objBounciness > 0) { jumpBuff = StartCoroutine(PostJumpBuffer()); }
        isInContact = false;
    }

    //Checks if the GameObjects in contact are below the controller
    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        curContact.Clear();
        Physics2D.GetContacts(col2D, collision.collider, filter.NoFilter(), curContact);

        for (int i = 0; i < curContact.Count; i++)
        {
            if (curContact[i].normal.y <= -.9f)
            {
                isInContact = true;
                isJumping = false;
                //if (jumpBuff != null) { StopCoroutine(PostJumpBuffer()); }
                break;
            }
        }

        if (collision.gameObject.CompareTag("TrapDoor"))
        {
            TrapDoor trapDoor = collision.gameObject.GetComponent<TrapDoor>();
            // If the player is on the trap door and press S
            if (trapDoor != null && moveInput.y < 0 && !trapDoor.IsTrapDoorBlocked())
            {
                trapDoor.OpenDoor();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Detection of the stairs
        if (collision.gameObject.tag == "Stair")
        {
            HandleStairClimbing(collision);
        }
    }

    // Manage the climbing of the stair for the player
    private void HandleStairClimbing(Collider2D collider)
    {
        StairController stair = collider.gameObject.GetComponent<StairController>();
        if (moveInput.y != 0 && canClimbAgain && stairDelay == null)
        {
            moveInput.x = 0f;
            rigid2D.linearVelocityX = 0f;
            canClimbAgain = false;
            stairDelay = StartCoroutine(StairClimbDelay());
            //StairDirection direction = moveInput.y > 0 ? StairDirection.Upward : StairDirection.Downward;
            StairDirection direction;
            if (stair.UpperFloor != null)
            {
                direction = StairDirection.Upward;
            }
            else
            {
                direction = StairDirection.Downward;
            }
            //StairDirection direction = StairDirection.Upward;
            StairController nextStair = direction == StairDirection.Upward ? stair.UpperFloor : stair.BottomFloor;
            if (nextStair != null)
            {
                // If the stairs are not blocked
                if (!stair.IsStairBlocked() && !nextStair.IsStairBlocked())
                {
                    stair.ClimbStair(this.gameObject, direction);
                }
            }            
                      
        }
    }
}