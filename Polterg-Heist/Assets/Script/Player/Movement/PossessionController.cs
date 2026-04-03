using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Interface for every object that can be possessed
public interface IPossessable
{
    void OnPossessed();
    void OnDepossessed();
}


/// But: Controller un objet possédé
/// Requiert: Rigidbody2D
/// Requiert Externe: tag "Wall"
/// Input: A = droite, D = gauche, SPACE = saut
/// État: Adéquat(temp)
public class PossessionController : MovementController, IPossessable, IResetInitialState
{
    private bool isMoving;
    private Vector2 lastPosition;
    private bool canObjectJump = false;
    public bool IsMoving;// { get { return isMoving; } private set { isMoving = value; } } // Is the object moving?

    public bool isClimbing;

    // Movement variables
    public Vector2 LastKnownPosition { get; private set; }          // Last known position of the object by an NPC
    public Quaternion LastKnownRotation { get; private set; }       // Last known rotation of the object by an NPC

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    float movementThreshold = 0.01f;    
    public enum objectType
    {
        Lightest = 6,
        Light = 5,
        Normal = 4,
        Heavy = 3,
        Heaviest = 2,
    }

    public objectType weightOfAnimation;

    protected override void Awake()
    {
        base.Awake();
        LastKnownPosition = transform.position;
        LastKnownRotation = transform.rotation;
    }

    protected override void Start()
    {
        base.Start();
        lastPosition = transform.position;
        canMove = false;
        canObjectJump = canJump;
        canJump = false;

        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    //Pour les inputs
    protected override void Update()
    {
        base.Update();
        if ((canMove && canWalk && moveInput.x != 0 && (Mathf.Abs(transform.position.x - lastPosition.x) > movementThreshold)) || (canMove && isJumping))
        {
            IsMoving = true;
            //IsMoving = (moveInput.x != 0 || isJumping) ? true : false;
        }
        else
        {
            IsMoving = false;
        }
            
        lastPosition = transform.position;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    // The NPC saw the object has moved from it's origin position/rotation so we update it's new position/rotation
    public void UpdateLastKnownPositionRotation()
    {
        LastKnownPosition = this.transform.position;
        LastKnownRotation = this.transform.rotation;
    }

    public void OnPossessed()
    {
        canMove = true;
        canJump = canObjectJump;
        isJumping = false;
    }

    public void OnDepossessed()
    {
        canMove = false;
    }

    public Vector2 GetMovementDirection()
    {
        if(rigid2D != null)
        {
            return rigid2D.linearVelocity.normalized;
        }
        return IsMoving ? ((Vector2)transform.position - lastPosition).normalized : Vector2.zero;
    }

    public void ResetInitialState()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        isClimbing = false;
        OnDepossessed();
        jumpBounceSoundEvent.Stop(gameObject);
        //jumpVelocityAdjustment = 0;
        //wantsToJump = false;
        //rigid2D.sharedMaterial = iniMat;
        StopAllCoroutines();

    }
}
